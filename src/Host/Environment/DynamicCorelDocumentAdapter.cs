using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using QiTuCDR.Core.Host;
using QiTuCDR.Core.Selection;
using QiTuCDR.Core.Tools.ConvertText;
using QiTuCDR.Infrastructure.COM;
using QiTuCDR.Infrastructure.Logging;
using QiTuCDR.Shared;

namespace QiTuCDR.Host.Environment
{
    public sealed class DynamicCorelDocumentAdapter : ICorelDocumentAdapter
    {
        private readonly CorelHostContext _hostContext;
        private readonly IComDispatcher _comDispatcher;
        private readonly ILogger _logger;

        public DynamicCorelDocumentAdapter(CorelHostContext hostContext, IComDispatcher comDispatcher, ILogger logger)
        {
            _hostContext = hostContext;
            _comDispatcher = comDispatcher;
            _logger = logger;
        }

        public bool HasOpenDocument => _hostContext.HasOpenDocument;

        public Task<SelectionSnapshot> CaptureSelectionSnapshotAsync(CancellationToken token)
        {
            return _comDispatcher.InvokeAsync<SelectionSnapshot>(() =>
            {
                dynamic? selection = null;
                var ids = new List<int>();

                try
                {
                    selection = _hostContext.Application?.ActiveSelectionRange;
                    if (selection == null)
                    {
                        return new SelectionSnapshot(ids);
                    }

                    ForEachShape((object)selection, token, shapeObject =>
                    {
                        dynamic shape = shapeObject;
                        ids.Add(SafeInt(() => shape.StaticID));
                    });

                    return new SelectionSnapshot(ids);
                }
                finally
                {
                    ReleaseComObject(selection);
                }
            }, token);
        }

        public Task<IReadOnlyList<CorelShapeSnapshot>> GetShapeSnapshotsAsync(ToolRange range, SelectionSnapshot? selectionSnapshot, bool includeHidden, CancellationToken token)
        {
            return _comDispatcher.InvokeAsync<IReadOnlyList<CorelShapeSnapshot>>(() =>
            {
                dynamic? shapeRange = null;
                var shapes = new List<CorelShapeSnapshot>();

                try
                {
                    shapeRange = ResolveShapeRange(range, selectionSnapshot);
                    if (shapeRange == null)
                    {
                        return shapes;
                    }

                    ForEachShape((object)shapeRange, token, shapeObject =>
                    {
                        dynamic shape = shapeObject;
                        var isHidden = SafeBool(() => !shape.Visible);
                        if (!includeHidden && isHidden)
                        {
                            return;
                        }

                        shapes.Add(new CorelShapeSnapshot(
                            SafeInt(() => shape.StaticID),
                            SafeBool(() => shape.Locked),
                            isHidden));
                    });

                    return shapes;
                }
                finally
                {
                    ReleaseComObject(shapeRange);
                }
            }, token);
        }

        public Task<bool> CanResolveSelectionSnapshotAsync(SelectionSnapshot selectionSnapshot, CancellationToken token)
        {
            return _comDispatcher.InvokeAsync<bool>(() =>
            {
                dynamic? shapeRange = null;

                try
                {
                    shapeRange = ResolveSelectionSnapshotRange(selectionSnapshot);
                    if (shapeRange == null)
                    {
                        return false;
                    }

                    object shapeRangeObject = shapeRange;
                    dynamic resolvedRange = shapeRangeObject;
                    return SafeInt(() => resolvedRange.Count) == selectionSnapshot.ShapeIds.Count;
                }
                finally
                {
                    ReleaseComObject(shapeRange);
                }
            }, token);
        }

        public Task<ConvertTextResult> ConvertTextAsync(ToolRange range, SelectionSnapshot? selectionSnapshot, bool includeHidden, int batchSize, Action<ConvertTextResult> progress, CancellationToken token)
        {
            return _comDispatcher.InvokeAsync<ConvertTextResult>(() =>
            {
                return ExecuteDocumentCommandGroup("QiTuCDR Convert Text", () =>
                {
                    dynamic? shapeRange = null;
                    var converted = 0;
                    var skipped = 0;

                    try
                    {
                        shapeRange = ResolveShapeRange(range, selectionSnapshot);
                        if (shapeRange == null)
                        {
                            return new ConvertTextResult(0, 0, 0);
                        }

                        var total = SafeInt(() => shapeRange.Count);
                        var progressTracker = new ConvertTextProgressTracker(batchSize, total, progress);
                        ForEachShape((object)shapeRange, token, shapeObject =>
                        {
                            dynamic shape = shapeObject;

                            if (!includeHidden && SafeBool(() => !shape.Visible))
                            {
                                skipped++;
                                progressTracker.ReportProcessed(converted, skipped);
                                return;
                            }

                            if (SafeBool(() => shape.Locked))
                            {
                                skipped++;
                                progressTracker.ReportProcessed(converted, skipped);
                                return;
                            }

                            try
                            {
                                shape.ConvertToCurves();
                                converted++;
                            }
                            catch (Exception ex)
                            {
                                skipped++;
                                _logger.Warn("Shape convert-to-curves skipped: " + ex.Message);
                            }

                            progressTracker.ReportProcessed(converted, skipped);
                        });

                        return new ConvertTextResult(converted, skipped, total);
                    }
                    finally
                    {
                        ReleaseComObject(shapeRange);
                    }
                });
            }, token);
        }

        public Task<int> CenterObjectsAsync(SelectionSnapshot selectionSnapshot, string mode, CancellationToken token)
        {
            return _comDispatcher.InvokeAsync<int>(() =>
            {
                return ExecuteDocumentCommandGroup("QiTuCDR Center Objects", () =>
                {
                    dynamic? range = null;
                    dynamic? page = null;

                    try
                    {
                        range = ResolveShapeRange(ToolRange.Selection, selectionSnapshot);
                        page = _hostContext.Application?.ActivePage;
                        if (range == null || page == null)
                        {
                            return 0;
                        }

                        return CenterResolvedRange(range, page, mode, token);
                    }
                    finally
                    {
                        ReleaseComObject(page);
                        ReleaseComObject(range);
                    }
                });
            }, token);
        }

        public Task<int> NormalizeSizeAsync(SelectionSnapshot selectionSnapshot, NormalizeSizeOptions options, CancellationToken token)
        {
            return _comDispatcher.InvokeAsync<int>(() =>
            {
                return ExecuteDocumentCommandGroup("QiTuCDR Normalize Size", () =>
                {
                    dynamic? range = null;
                    var count = 0;

                    try
                    {
                        range = ResolveShapeRange(ToolRange.Selection, selectionSnapshot);
                        if (range == null)
                        {
                            return 0;
                        }

                        ForEachShape((object)range, token, shapeObject =>
                        {
                            dynamic shape = shapeObject;

                            ApplyNormalizeSize(shape, options);
                            count++;
                        });

                        return count;
                    }
                    finally
                    {
                        ReleaseComObject(range);
                    }
                });
            }, token);
        }

        private static int CenterResolvedRange(object rangeObject, object pageObject, string mode, CancellationToken token)
        {
            dynamic activeRange = rangeObject;
            dynamic activePage = pageObject;
            var centerX = SafeDouble(() => activePage.SizeWidth) / 2;
            var centerY = SafeDouble(() => activePage.SizeHeight) / 2;

            if (mode == "individual")
            {
                var count = 0;
                ForEachShape((object)activeRange, token, shapeObject =>
                {
                    dynamic shape = shapeObject;
                    shape.CenterX = centerX;
                    shape.CenterY = centerY;
                    count++;
                });

                return count;
            }

            activeRange.CenterX = centerX;
            activeRange.CenterY = centerY;
            return SafeInt(() => activeRange.Count);
        }

        private static void ApplyNormalizeSize(object shapeObject, NormalizeSizeOptions options)
        {
            dynamic shape = shapeObject;

            if (options.LockRatio)
            {
                if (options.Width != null)
                {
                    shape.SizeWidth = options.Width.Value;
                }
                else if (options.Height != null)
                {
                    shape.SizeHeight = options.Height.Value;
                }
            }
            else
            {
                if (options.Width != null)
                {
                    shape.SizeWidth = options.Width.Value;
                }

                if (options.Height != null)
                {
                    shape.SizeHeight = options.Height.Value;
                }
            }

            if (options.OutlineWidth != null)
            {
                dynamic? outline = null;
                try
                {
                    outline = shape.Outline;
                    if (outline != null)
                    {
                        outline.Width = options.OutlineWidth.Value;
                    }
                }
                finally
                {
                    ReleaseComObject(outline);
                }
            }
        }

        public Task<int> CleanupRedundantAsync(CancellationToken token)
        {
            return _comDispatcher.InvokeAsync<int>(() =>
            {
                return ExecuteDocumentCommandGroup("QiTuCDR Cleanup Redundant", () =>
                {
                    var removed = 0;

                    token.ThrowIfCancellationRequested();
                    removed += DeleteActivePageGuides(token);

                    token.ThrowIfCancellationRequested();
                    removed += DeleteHiddenEmptyLayers(token);

                    token.ThrowIfCancellationRequested();
                    removed += DeleteEmptyTextShapes(token);

                    return removed;
                });
            }, token);
        }

        private int DeleteActivePageGuides(CancellationToken token)
        {
            dynamic? page = null;
            dynamic? guidelines = null;
            dynamic? guides = null;

            try
            {
                page = _hostContext.Application?.ActivePage;
                if (page == null)
                {
                    return 0;
                }

                var removed = 0;
                guidelines = SafeObject(() => page.Guidelines);
                removed += DeleteCollectionItems(guidelines, token);

                guides = SafeObject(() => page.Guides);
                removed += DeleteCollectionItems(guides, token);
                return removed;
            }
            finally
            {
                ReleaseComObject(guides);
                ReleaseComObject(guidelines);
                ReleaseComObject(page);
            }
        }

        private int DeleteHiddenEmptyLayers(CancellationToken token)
        {
            dynamic? document = null;
            dynamic? pages = null;
            var removed = 0;

            try
            {
                document = _hostContext.Application?.ActiveDocument;
                if (document == null)
                {
                    return 0;
                }

                pages = document.Pages;
                foreach (object pageObject in pages)
                {
                    token.ThrowIfCancellationRequested();
                    dynamic? layers = null;

                    try
                    {
                        dynamic page = pageObject;
                        layers = page.Layers;
                        foreach (object layerObject in layers)
                        {
                            token.ThrowIfCancellationRequested();

                            try
                            {
                                dynamic layer = layerObject;
                                if (SafeBool(() => !layer.Visible) && IsLayerEmpty(layer))
                                {
                                    layer.Delete();
                                    removed++;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Warn("Hidden empty layer cleanup skipped: " + ex.Message);
                            }
                            finally
                            {
                                ReleaseComObject(layerObject);
                            }
                        }
                    }
                    finally
                    {
                        ReleaseComObject(layers);
                        ReleaseComObject(pageObject);
                    }
                }

                return removed;
            }
            finally
            {
                ReleaseComObject(pages);
                ReleaseComObject(document);
            }
        }

        private int DeleteEmptyTextShapes(CancellationToken token)
        {
            dynamic? range = null;
            var removed = 0;

            try
            {
                range = ResolveShapeRange(ToolRange.Document, null);
                if (range == null)
                {
                    return 0;
                }

                ForEachShape((object)range, token, shapeObject =>
                {
                    dynamic shape = shapeObject;
                    if (!IsEmptyTextShape(shape))
                    {
                        return;
                    }

                    try
                    {
                        shape.Delete();
                        removed++;
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn("Empty text cleanup skipped: " + ex.Message);
                    }
                });

                return removed;
            }
            finally
            {
                ReleaseComObject(range);
            }
        }

        private int DeleteCollectionItems(object? collectionObject, CancellationToken token)
        {
            if (collectionObject == null)
            {
                return 0;
            }

            dynamic collection = collectionObject;
            var removed = 0;
            foreach (object itemObject in collection)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    dynamic item = itemObject;
                    item.Delete();
                    removed++;
                }
                catch (Exception ex)
                {
                    _logger.Warn("Redundant collection item cleanup skipped: " + ex.Message);
                }
                finally
                {
                    ReleaseComObject(itemObject);
                }
            }

            return removed;
        }

        private dynamic? ResolveShapeRange(ToolRange range, SelectionSnapshot? selectionSnapshot)
        {
            var app = _hostContext.Application;
            if (app == null)
            {
                return null;
            }

            switch (range)
            {
                case ToolRange.Selection:
                    return selectionSnapshot != null && selectionSnapshot.ShapeIds.Count > 0
                        ? ResolveSelectionSnapshotRange(selectionSnapshot)
                        : app.ActiveSelectionRange;
                case ToolRange.CurrentPage:
                    return ResolveCurrentPageShapeRange(app);
                case ToolRange.Document:
                    return ResolveDocumentShapeRange(app);
                default:
                    return null;
            }
        }

        private dynamic? ResolveSelectionSnapshotRange(SelectionSnapshot selectionSnapshot)
        {
            var app = _hostContext.Application;
            if (app == null || selectionSnapshot.ShapeIds.Count == 0)
            {
                return null;
            }

            dynamic? range = null;

            try
            {
                range = CreateEmptyShapeRange(app);
                foreach (var shapeId in selectionSnapshot.ShapeIds)
                {
                    dynamic? shape = null;
                    try
                    {
                        shape = FindShapeByStaticId(shapeId);
                        if (shape != null)
                        {
                            AddShapeToRange(range, shape);
                        }
                    }
                    finally
                    {
                        ReleaseComObject(shape);
                    }
                }

                if (SafeInt(() => range.Count) == 0)
                {
                    ReleaseComObject(range);
                    return null;
                }

                return range;
            }
            catch
            {
                ReleaseComObject(range);
                throw;
            }
        }

        private dynamic? FindShapeByStaticId(int staticId)
        {
            var app = _hostContext.Application;
            if (app == null)
            {
                return null;
            }

            try
            {
                return FindShapeOnActivePage(app, staticId);
            }
            catch
            {
                try
                {
                    return FindShapeInActiveDocument(app, staticId);
                }
                catch
                {
                    return null;
                }
            }
        }

        private T ExecuteDocumentCommandGroup<T>(string name, Func<T> action)
        {
            var app = _hostContext.Application;
            if (app == null)
            {
                return action();
            }

            dynamic? document = null;
            var started = false;

            try
            {
                document = app.ActiveDocument;
                if (document != null)
                {
                    try
                    {
                        document!.BeginCommandGroup(name);
                        started = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn("Begin CorelDRAW command group failed: " + ex.Message);
                    }
                }

                return action();
            }
            finally
            {
                if (started && document != null)
                {
                    try
                    {
                        document!.EndCommandGroup();
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn("End CorelDRAW command group failed: " + ex.Message);
                    }
                }

                ReleaseComObject(document);
            }
        }

        private static dynamic? ResolveCurrentPageShapeRange(object application)
        {
            dynamic app = application;
            dynamic? page = null;
            dynamic? shapes = null;

            try
            {
                page = app.ActivePage;
                if (page == null)
                {
                    return null;
                }

                shapes = page.Shapes;
                return shapes?.All();
            }
            finally
            {
                ReleaseComObject(shapes);
                ReleaseComObject(page);
            }
        }

        private static dynamic? ResolveDocumentShapeRange(object application)
        {
            dynamic app = application;
            dynamic? document = null;
            dynamic? shapes = null;

            try
            {
                document = app.ActiveDocument;
                if (document == null)
                {
                    return null;
                }

                shapes = document.Shapes;
                return shapes?.All();
            }
            finally
            {
                ReleaseComObject(shapes);
                ReleaseComObject(document);
            }
        }

        private static dynamic? FindShapeOnActivePage(object application, int staticId)
        {
            dynamic app = application;
            dynamic? page = null;
            dynamic? shapes = null;

            try
            {
                page = app.ActivePage;
                if (page == null)
                {
                    return null;
                }

                shapes = page.Shapes;
                return shapes?.FindShape(string.Empty, 0, staticId, true, string.Empty);
            }
            finally
            {
                ReleaseComObject(shapes);
                ReleaseComObject(page);
            }
        }

        private static dynamic? FindShapeInActiveDocument(object application, int staticId)
        {
            dynamic app = application;
            dynamic? document = null;
            dynamic? shapes = null;

            try
            {
                document = app.ActiveDocument;
                if (document == null)
                {
                    return null;
                }

                shapes = document.Shapes;
                return shapes?.FindShape(string.Empty, 0, staticId, true, string.Empty);
            }
            finally
            {
                ReleaseComObject(shapes);
                ReleaseComObject(document);
            }
        }

        private static dynamic CreateEmptyShapeRange(object application)
        {
            dynamic corelApp = application;
            return corelApp.CreateShapeRange();
        }

        private static void AddShapeToRange(object shapeRange, object shape)
        {
            dynamic range = shapeRange;
            dynamic resolvedShape = shape;
            range.Add(resolvedShape);
        }

        private static void ForEachShape(object rangeObject, CancellationToken token, Action<object> action)
        {
            dynamic range = rangeObject;
            dynamic? shapeCollection = null;

            try
            {
                shapeCollection = range.Shapes;
                if (shapeCollection == null)
                {
                    return;
                }

                foreach (object shapeObject in shapeCollection)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        action(shapeObject);
                    }
                    finally
                    {
                        ReleaseComObject(shapeObject);
                    }
                }
            }
            finally
            {
                ReleaseComObject(shapeCollection);
            }
        }

        private static void ReleaseComObject(object? value)
        {
            if (value != null && Marshal.IsComObject(value))
            {
                Marshal.ReleaseComObject(value);
            }
        }

        private static int SafeInt(Func<dynamic> read)
        {
            try
            {
                return (int)read();
            }
            catch
            {
                return 0;
            }
        }

        private static double SafeDouble(Func<dynamic> read)
        {
            try
            {
                return (double)read();
            }
            catch
            {
                return 0;
            }
        }

        private static bool SafeBool(Func<dynamic> read)
        {
            try
            {
                return (bool)read();
            }
            catch
            {
                return false;
            }
        }

        private static object? SafeObject(Func<dynamic> read)
        {
            try
            {
                return read();
            }
            catch
            {
                return null;
            }
        }

        private static string SafeString(Func<dynamic> read)
        {
            try
            {
                return (string)read();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool IsLayerEmpty(object layerObject)
        {
            dynamic layer = layerObject;
            dynamic? shapes = null;

            try
            {
                shapes = layer.Shapes;
                if (shapes == null)
                {
                    return true;
                }

                dynamic shapeCollection = shapes;
                return SafeInt(() => shapeCollection.Count) == 0;
            }
            finally
            {
                ReleaseComObject(shapes);
            }
        }

        private static bool IsEmptyTextShape(object shapeObject)
        {
            dynamic shape = shapeObject;
            dynamic? text = null;
            dynamic? story = null;

            try
            {
                text = SafeObject(() => shape.Text);
                if (text == null)
                {
                    return false;
                }

                story = SafeObject(() => text.Story);
                var value = story != null
                    ? SafeString(() => story.Text)
                    : SafeString(() => text.Contents);
                return string.IsNullOrWhiteSpace(value);
            }
            finally
            {
                ReleaseComObject(story);
                ReleaseComObject(text);
            }
        }
    }
}
