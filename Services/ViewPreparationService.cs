using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using BimTools.SpxExporter.Infrastructure;
using BimTools.SpxExporter.Models;

namespace BimTools.SpxExporter.Services
{
    public sealed class ViewPreparationService
    {
        public string Apply(Document doc, View3D view, ExportSettings settings, bool isTemporary)
        {
            if (!isTemporary && !settings.PrepareExistingViews)
            {
                return string.Empty;
            }

            List<string> notes = new List<string>();
            using (Transaction transaction = new Transaction(doc, "BIM TOOLS - Prepare export view"))
            {
                transaction.Start();
                try
                {
                    ApplyDetailLevel(view, settings);
                    ApplyDisplayStyle(view, settings, notes);
                    ApplyViewOptions(view, settings, notes);
                    HideCategoryGroups(doc, view, settings, notes);
                    ApplyTransparency(doc, view, settings, notes);
                    HideCategories(doc, view, settings, notes);
                    ApplyPhase(doc, view, settings, notes);
                    HideWorksets(doc, view, settings, notes);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    return "View preparation failed: " + ex.Message;
                }
            }

            return notes.Count == 0 ? string.Empty : string.Join(" ", notes);
        }

        private static void ApplyDetailLevel(View3D view, ExportSettings settings)
        {
            switch (settings.ViewDetailLevel)
            {
                case DetailLevelOption.Coarse:
                    view.DetailLevel = ViewDetailLevel.Coarse;
                    break;
                case DetailLevelOption.Medium:
                    view.DetailLevel = ViewDetailLevel.Medium;
                    break;
                case DetailLevelOption.Fine:
                    view.DetailLevel = ViewDetailLevel.Fine;
                    break;
            }
        }

        private static void ApplyDisplayStyle(View3D view, ExportSettings settings, List<string> notes)
        {
            DisplayStyle target = view.DisplayStyle;
            switch (settings.ViewDisplayStyle)
            {
                case DisplayStyleOption.Wireframe:
                    target = DisplayStyle.Wireframe;
                    break;
                case DisplayStyleOption.HiddenLine:
                    target = DisplayStyle.HLR;
                    break;
                case DisplayStyleOption.Shaded:
                    target = DisplayStyle.Shading;
                    break;
                case DisplayStyleOption.ConsistentColors:
                    target = DisplayStyle.FlatColors;
                    break;
                case DisplayStyleOption.Textures:
                    // DisplayStyle.Textures was introduced in Revit 2022+.
                    // We use Realistic as a safe fallback when building against older API stubs.
                    target = DisplayStyle.Realistic;
                    break;
                case DisplayStyleOption.Realistic:
                    target = DisplayStyle.Realistic;
                    break;
            }

            if (settings.ViewShowEdges != ToggleOption.Keep
                && (target == DisplayStyle.Shading || target == DisplayStyle.ShadingWithEdges))
            {
                target = settings.ViewShowEdges == ToggleOption.On ? DisplayStyle.ShadingWithEdges : DisplayStyle.Shading;
            }

            if (target == view.DisplayStyle)
            {
                return;
            }

            try
            {
                view.DisplayStyle = target;
                notes.Add("Display style " + target + ".");
            }
            catch (Exception ex)
            {
                notes.Add("Display style not applied: " + ex.Message);
            }
        }

        private static void ApplyViewOptions(View3D view, ExportSettings settings, List<string> notes)
        {
            if (settings.ViewDiscipline != DisciplineOption.Keep)
            {
                int value = 4095;
                switch (settings.ViewDiscipline)
                {
                    case DisciplineOption.Architectural:
                        value = 1;
                        break;
                    case DisciplineOption.Structural:
                        value = 2;
                        break;
                    case DisciplineOption.Mechanical:
                        value = 4;
                        break;
                    case DisciplineOption.Electrical:
                        value = 8;
                        break;
                }

                SetIntegerParameter(view, BuiltInParameter.VIEW_DISCIPLINE, value, "Discipline", notes);
            }

            if (settings.ViewPartsVisibility != PartsVisibilityOption.Keep)
            {
                int value = 0;
                switch (settings.ViewPartsVisibility)
                {
                    case PartsVisibilityOption.ShowOriginal:
                        value = 1;
                        break;
                    case PartsVisibilityOption.ShowBoth:
                        value = 2;
                        break;
                }

                SetIntegerParameter(view, BuiltInParameter.VIEW_PARTS_VISIBILITY, value, "Parts visibility", notes);
            }

            if (settings.ViewSectionBox != ToggleOption.Keep)
            {
                try
                {
                    bool active = settings.ViewSectionBox == ToggleOption.On;
                    if (view.IsSectionBoxActive != active)
                    {
                        view.IsSectionBoxActive = active;
                        notes.Add("Section box " + (active ? "on." : "off."));
                    }
                }
                catch (Exception ex)
                {
                    notes.Add("Section box not changed: " + ex.Message);
                }
            }
        }

        private static void SetIntegerParameter(View3D view, BuiltInParameter id, int value, string label, List<string> notes)
        {
            try
            {
                Parameter parameter = view.get_Parameter(id);
                if (parameter == null || parameter.IsReadOnly || parameter.StorageType != StorageType.Integer)
                {
                    notes.Add(label + " is not available on this view.");
                    return;
                }

                if (parameter.AsInteger() != value)
                {
                    parameter.Set(value);
                    notes.Add(label + " set.");
                }
            }
            catch (Exception ex)
            {
                notes.Add(label + " not applied: " + ex.Message);
            }
        }

        private static void HideCategoryGroups(Document doc, View3D view, ExportSettings settings, List<string> notes)
        {
            try
            {
                if (settings.ViewHideAnnotations && !view.AreAnnotationCategoriesHidden)
                {
                    view.AreAnnotationCategoriesHidden = true;
                    notes.Add("Annotation categories hidden.");
                }

                if (settings.ViewHideImports && !view.AreImportCategoriesHidden)
                {
                    view.AreImportCategoriesHidden = true;
                    notes.Add("Imported categories hidden.");
                }
            }
            catch (Exception ex)
            {
                notes.Add("Category groups not hidden: " + ex.Message);
            }

            if (!settings.ViewHideAnalytical)
            {
                return;
            }

            int hidden = 0;
            foreach (Category category in doc.Settings.Categories)
            {
                try
                {
                    if (category.CategoryType == CategoryType.AnalyticalModel && view.CanCategoryBeHidden(category.Id))
                    {
                        view.SetCategoryHidden(category.Id, true);
                        hidden++;
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }

            if (hidden > 0)
            {
                notes.Add("Analytical model categories hidden (" + hidden + ").");
            }
        }

        private static void ApplyTransparency(Document doc, View3D view, ExportSettings settings, List<string> notes)
        {
            int transparency = settings.ViewSurfaceTransparency;
            if (transparency <= 0)
            {
                return;
            }

            int changed = 0;
            foreach (Category category in doc.Settings.Categories)
            {
                try
                {
                    if (category.CategoryType != CategoryType.Model
                        || !view.CanCategoryBeHidden(category.Id)
                        || view.GetCategoryHidden(category.Id))
                    {
                        continue;
                    }

                    OverrideGraphicSettings overrides = view.GetCategoryOverrides(category.Id);
                    overrides.SetSurfaceTransparency(transparency);
                    view.SetCategoryOverrides(category.Id, overrides);
                    changed++;
                }
                catch (Exception)
                {
                    continue;
                }
            }

            if (changed > 0)
            {
                notes.Add("Surface transparency " + transparency + "% on " + changed + " categories.");
            }
        }

        private static void HideCategories(Document doc, View3D view, ExportSettings settings, List<string> notes)
        {
            List<ElementId> ids = new List<ElementId>();
            if (settings.ViewHideRooms)
            {
                ids.Add(new ElementId(BuiltInCategory.OST_Rooms));
            }

            if (settings.ViewHideGridsAndLevels)
            {
                ids.Add(new ElementId(BuiltInCategory.OST_Grids));
                ids.Add(new ElementId(BuiltInCategory.OST_Levels));
            }

            if (settings.ViewHideLinks)
            {
                ids.Add(new ElementId(BuiltInCategory.OST_RvtLinks));
            }

            if (settings.ViewHideViewMarkers)
            {
                ids.Add(new ElementId(BuiltInCategory.OST_Cameras));
                ids.Add(new ElementId(BuiltInCategory.OST_Viewers));
            }

            string[] names = PatternMatcher.Split(settings.ViewHideCategoryNames);
            if (names.Length > 0)
            {
                foreach (Category category in doc.Settings.Categories)
                {
                    if (PatternMatcher.MatchesAny(category.Name, names))
                    {
                        ids.Add(category.Id);
                    }
                }
            }

            int hidden = 0;
            foreach (ElementId id in ids)
            {
                try
                {
                    if (view.CanCategoryBeHidden(id))
                    {
                        view.SetCategoryHidden(id, true);
                        hidden++;
                    }
                }
                catch (Autodesk.Revit.Exceptions.ArgumentException)
                {
                    continue;
                }
            }

            if (hidden > 0)
            {
                notes.Add("Hid " + hidden + " categor" + (hidden == 1 ? "y." : "ies."));
            }
        }

        private static void ApplyPhase(Document doc, View3D view, ExportSettings settings, List<string> notes)
        {
            if (!string.IsNullOrWhiteSpace(settings.ViewPhaseName))
            {
                Parameter phaseParameter = view.get_Parameter(BuiltInParameter.VIEW_PHASE);
                if (phaseParameter != null && !phaseParameter.IsReadOnly)
                {
                    foreach (Phase phase in doc.Phases)
                    {
                        if (string.Equals(phase.Name, settings.ViewPhaseName.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            phaseParameter.Set(phase.Id);
                            notes.Add("Phase '" + phase.Name + "'.");
                            break;
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(settings.ViewPhaseFilterName))
            {
                Parameter filterParameter = view.get_Parameter(BuiltInParameter.VIEW_PHASE_FILTER);
                if (filterParameter != null && !filterParameter.IsReadOnly)
                {
                    using (FilteredElementCollector collector = new FilteredElementCollector(doc))
                    {
                        collector.OfClass(typeof(PhaseFilter));
                        foreach (Element element in collector)
                        {
                            if (string.Equals(element.Name, settings.ViewPhaseFilterName.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                filterParameter.Set(element.Id);
                                notes.Add("Phase filter '" + element.Name + "'.");
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static void HideWorksets(Document doc, View3D view, ExportSettings settings, List<string> notes)
        {
            string[] patterns = PatternMatcher.Split(settings.ViewHideWorksetPatterns);
            if (patterns.Length == 0 || !doc.IsWorkshared)
            {
                return;
            }

            int hidden = 0;
            FilteredWorksetCollector collector = new FilteredWorksetCollector(doc);
            try
            {
                collector.OfKind(WorksetKind.UserWorkset);
                foreach (Workset workset in collector)
                {
                    if (PatternMatcher.MatchesAny(workset.Name, patterns))
                    {
                        view.SetWorksetVisibility(workset.Id, WorksetVisibility.Hidden);
                        hidden++;
                    }
                }
            }
            finally
            {
                Disposer.Release(collector);
            }

            if (hidden > 0)
            {
                notes.Add("Hid " + hidden + " workset(s) in the export view.");
            }
        }
    }
}
