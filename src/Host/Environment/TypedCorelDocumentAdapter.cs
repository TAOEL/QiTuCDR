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
using VGCore;

namespace QiTuCDR.Host.Environment
{
    public sealed class TypedCorelDocumentAdapter : ICorelDocumentAdapter
    {
        private readonly Application _application;
        private readonly IComDispatcher _comDispatcher;
        private readonly ILogger _logger;

        public TypedCorelDocumentAdapter(object corelApplication, IComDispatcher comDispatcher, ILogger logger)
        {
            _application = (Application)corelApplication;
            _comDispatcher = comDispatcher;
            _logger = logger;
        }

        public bool HasOpenDocument
        {
            get
            {
                Document? document = null;
                try
                {
                    document = _application.ActiveDocument;
                    return document != null;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    ReleaseComObject(document);
                }
            }
        }

        public Task<SelectionSnapshot> CaptureSelectionSnapshotAsync(CancellationToken token)
        {
            return _comDispatcher.InvokeAsync(() =>
            {
                ShapeRange? range = null;
                var ids = new List<int>();

                try
                {
                    range = _application.ActiveSelectionRange;
                    if (range == null)
                    {
                        return new SelectionSnapshot(ids);
                    }

                    ForEachShape(range, token, shape => ids.Add(SafeInt(() => shape.StaticID)));
                    return new SelectionSnapshot(ids);
                }
                finally
                {
                    ReleaseComObject(range);
                }
            }, token);
        }

        public Task<IReadOnlyList<CorelShapeSnapshot>> GetShapeSnapshotsAsync(ToolRange range, SelectionSnapshot? selectionSnapshot, bool includeHidden, CancellationToken token)
        {
            return _comDispatcher.InvokeAsync<IReadOnlyList<CorelShapeSnapshot>>(() =>
            {
                ShapeRange? shapeRange = null;
                var shapes = new List<CorelShapeSnapshot>();

                try
                {
                    shapeRange = ResolveShapeRange(range, selectionSnapshot);
                    if (shapeRange == null)
                    {
                        return shapes;
                    }

                    ForEachShape(shapeRange, token, shape =>
                    {
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
            return _comDispatcher.InvokeAsync(() =>
            {
                ShapeRange? range = null;

                try
                {
                    range = ResolveSelectionSnapshotRange(selectionSnapshot);
                    return range != null && SafeInt(() => range.Count) == selectionSnapshot.ShapeIds.Count;
                }
                finally
                {
                    ReleaseComObject(range);
                }
            }, token);
        }

        public Task<ConvertTextResult> ConvertTextAsync(ToolRange range, SelectionSnapshot? selectionSnapshot, bool includeHidden, int batchSize, Action<ConvertTextResult> progress, CancellationToken token)
        {
            return _comDispatcher.InvokeAsync(() =>
            {
                return ExecuteDocumentCommandGroup("QiTuCDR Convert Text", () =>
                {
                    ShapeRange? shapeRange = null;
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
                        ForEachShape(shapeRange, token, shape =>
                        {
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
            return _comDispatcher.InvokeAsync(() =>
            {
                return ExecuteDocumentCommandGroup("QiTuCDR Center Objects", () =>
                {
                    ShapeRange? range = null;
                    Page? page = null;

                    try
                    {
                        range = ResolveShapeRange(ToolRange.Selection, selectionSnapshot);
                        page = _application.ActivePage;
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
            return _comDispatcher.InvokeAsync(() =>
            {
                return ExecuteDocumentCommandGroup("QiTuCDR Normalize Size", () =>
                {
                    ShapeRange? range = null;
                    var count = 0;

                    try
                    {
                        range = ResolveShapeRange(ToolRange.Selection, selectionSnapshot);
                        if (range == null)
                        {
                            return 0;
                        }

                        ForEachShape(range, token, shape =>
                        {
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

        public Task<int> CleanupRedundantAsync(CancellationToken token)
        {
            return _comDispatcher.InvokeAsync(() =>
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
            Page? page = null;
            dynamic? guidelines = null;
            dynamic? guides = null;

            try
            {
                page = _application.ActivePage;
                if (page == null)
                {
                    return 0;
                }

                dynamic pageObject = page;
                var removed = 0;
                guidelines = SafeObject(() => pageObject.Guidelines);
                removed += DeleteCollectionItems(guidelines, token);

                guides = SafeObject(() => pageObject.Guides);
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
            Document? document = null;
            dynamic? pages = null;
            var removed = 0;

            try
            {
                document = _application.ActiveDocument;
                if (document == null)
                {
                    return 0;
                }

                dynamic documentObject = document;
                pages = documentObject.Pages;
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
                                if (SafeDynamicBool(() => !layer.Visible) && IsLayerEmpty(layer))
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
            ShapeRange? range = null;
            var removed = 0;

            try
            {
                range = ResolveShapeRange(ToolRange.Document, null);
                if (range == null)
                {
                    return 0;
                }

                ForEachShape(range, token, shape =>
                {
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

        private static int CenterResolvedRange(ShapeRange range, Page page, string mode, CancellationToken token)
        {
            var centerX = SafeDouble(() => page.SizeWidth) / 2;
            var centerY = SafeDouble(() => page.SizeHeight) / 2;

            if (mode == "individual")
            {
                var count = 0;
                ForEachShape(range, token, shape =>
                {
                    shape.CenterX = centerX;
                    shape.CenterY = centerY;
                    count++;
                });

                return count;
            }

            range.CenterX = centerX;
            range.CenterY = centerY;
            return SafeInt(() => range.Count);
        }

        private static void ApplyNormalizeSize(Shape shape, NormalizeSizeOptions options)
        {
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
                Outline? outline = null;
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

        private ShapeRange? ResolveShapeRange(ToolRange range, SelectionSnapshot? selectionSnapshot)
        {
            switch (range)
            {
                case ToolRange.Selection:
                    return selectionSnapshot != null && selectionSnapshot.ShapeIds.Count > 0
                        ? ResolveSelectionSnapshotRange(selectionSnapshot)
                        : _application.ActiveSelectionRange;
                case ToolRange.CurrentPage:
                    return ResolveCurrentPageShapeRange();
                case ToolRange.Document:
                    return ResolveDocumentShapeRange();
                default:
                    return null;
            }
        }

        private ShapeRange? ResolveCurrentPageShapeRange()
        {
            Page? page = null;
            Shapes? shapes = null;

            try
            {
                page = _application.ActivePage;
                shapes = page?.Shapes;
                return shapes?.All();
            }
            finally
            {
                ReleaseComObject(shapes);
                ReleaseComObject(page);
            }
        }

        private ShapeRange? ResolveDocumentShapeRange()
        {
            Document? document = null;
            Shapes? shapes = null;

            try
            {
                document = _application.ActiveDocument;
                shapes = document?.SelectableShapes;
                return shapes?.All();
            }
            finally
            {
                ReleaseComObject(shapes);
                ReleaseComObject(document);
            }
        }

        private ShapeRange? ResolveSelectionSnapshotRange(SelectionSnapshot selectionSnapshot)
        {
            if (selectionSnapshot.ShapeIds.Count == 0)
            {
                return null;
            }

            ShapeRange? range = null;

            try
            {
                range = _application.CreateShapeRange();
                foreach (var shapeId in selectionSnapshot.ShapeIds)
                {
                    Shape? shape = null;
                    try
                    {
                        shape = FindShapeByStaticId(shapeId);
                        if (shape != null)
                        {
                            range.Add(shape);
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

        private Shape? FindShapeByStaticId(int staticId)
        {
            try
            {
                return FindShapeOnActivePage(staticId);
            }
            catch
            {
                try
                {
                    return FindShapeInActiveDocument(staticId);
                }
                catch
                {
                    return null;
                }
            }
        }

        private T ExecuteDocumentCommandGroup<T>(string name, Func<T> action)
        {
            Document? document = null;
            var started = false;

            try
            {
                document = _application.ActiveDocument;
                if (document != null)
                {
                    try
                    {
                        document.BeginCommandGroup(name);
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
                        document.EndCommandGroup();
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn("End CorelDRAW command group failed: " + ex.Message);
                    }
                }

                ReleaseComObject(document);
            }
        }

        private Shape? FindShapeOnActivePage(int staticId)
        {
            Page? page = null;
            Shapes? shapes = null;

            try
            {
                page = _application.ActivePage;
                shapes = page?.Shapes;
                return shapes?.FindShape(string.Empty, cdrShapeType.cdrNoShape, staticId, true, string.Empty);
            }
            finally
            {
                ReleaseComObject(shapes);
                ReleaseComObject(page);
            }
        }

        private Shape? FindShapeInActiveDocument(int staticId)
        {
            Document? document = null;
            Shapes? shapes = null;

            try
            {
                document = _application.ActiveDocument;
                shapes = document?.SelectableShapes;
                return shapes?.FindShape(string.Empty, cdrShapeType.cdrNoShape, staticId, true, string.Empty);
            }
            finally
            {
                ReleaseComObject(shapes);
                ReleaseComObject(document);
            }
        }

        private static void ForEachShape(ShapeRange range, CancellationToken token, Action<Shape> action)
        {
            Shapes? shapes = null;

            try
            {
                shapes = range.Shapes;
                if (shapes == null)
                {
                    return;
                }

                foreach (object shapeObject in shapes)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        action((Shape)shapeObject);
                    }
                    finally
                    {
                        ReleaseComObject(shapeObject);
                    }
                }
            }
            finally
            {
                ReleaseComObject(shapes);
            }
        }

        private static void ReleaseComObject(object? value)
        {
            if (value != null && Marshal.IsComObject(value))
            {
                Marshal.ReleaseComObject(value);
            }
        }

        private static int SafeInt(Func<int> read)
        {
            try
            {
                return read();
            }
            catch
            {
                return 0;
            }
        }

        private static double SafeDouble(Func<double> read)
        {
            try
            {
                return read();
            }
            catch
            {
                return 0;
            }
        }

        private static bool SafeBool(Func<bool> read)
        {
            try
            {
                return read();
            }
            catch
            {
                return false;
            }
        }

        private static int SafeDynamicInt(Func<dynamic> read)
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

        private static bool SafeDynamicBool(Func<dynamic> read)
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
                return SafeDynamicInt(() => shapeCollection.Count) == 0;
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
