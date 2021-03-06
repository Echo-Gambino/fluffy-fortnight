﻿namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Events;

    using MessageBox = System.Windows.Forms.MessageBox;
    using FilterMode = AdvAdvFilter.Common.FilterMode;

    /// <summary>
    /// RevitController performs any complex tasks regarding the 
    ///     Revit API in the execution of the ModelessForm.
    /// </summary>
    public class RevitController
    {
        #region Fields

        // Essential fields, often used and should never be null
        private ExternalCommandData commandData;
        private UIApplication uiApp;
        private UIDocument uiDoc;
        private Document doc;

        // Specialized fields, used on specific methods
        private View view;

        #endregion Fields

        #region Parameters

        public ExternalCommandData CommandData
        {
            get { return this.commandData; }
        }

        public UIApplication UiApp
        {
            get { return this.uiApp; }
        }

        public UIDocument UiDoc
        {
            get { return this.uiDoc; }
        }

        public Document Doc
        {
            get { return this.doc; }
        }

        public View View
        {
            get { return this.view; }
        }

        #endregion Parameters

        #region Public Methods

        public RevitController(ExternalCommandData commandData)
        {
            this.commandData = commandData;
            this.uiApp = commandData.Application;
            this.uiDoc = this.uiApp.ActiveUIDocument;
            this.doc = this.uiDoc.Document;

            this.view = null;
        }

        #region ElementId Getters

        public List<ElementId> GetAllElementIds(FilterMode filter)
        {
            List<ElementId> output = null;

            switch (filter)
            {
                case FilterMode.Selection:
                    output = GetElementIdsFromSelection();
                    break;
                case FilterMode.View:
                    output = GetElementIdsFromView();
                    break;
                case FilterMode.Project:
                    output = GetElementIdsFromDocument();
                    break;
                default:
                    break;
            }

            return output;
        }

        public List<ElementId> GetElementIdsFromDocument()
        {
            List<ElementId> output = null;

            try
            {
                FilteredElementCollector collection = new FilteredElementCollector(this.doc);

                collection.WherePasses(
                    new LogicalOrFilter(
                        new ElementIsElementTypeFilter(false),
                        new ElementIsElementTypeFilter(true)));

                output = collection.ToElementIds().ToList<ElementId>();
            }
            catch (ArgumentNullException ex)
            {
                ErrorReport.Report(ex);
            }
            catch (InvalidOperationException ex)
            {
                ErrorReport.Report(ex);
            }
            catch (Exception ex)
            {
                ErrorReport.Report(ex);
            }

            return output;
        }

        /// <summary>
        /// Get all element ids from this.view, returns null if unsuccessful
        /// </summary>
        /// <returns>All elements within the view</returns>
        public List<ElementId> GetElementIdsFromView()
        {
            List<ElementId> output = null;

            if (this.view == null)
                return output;

            try
            {
                FilteredElementCollector collector = new FilteredElementCollector(this.doc, this.View.Id);
                output = collector.ToElementIds().ToList<ElementId>();
            }
            catch (ArgumentNullException ex)
            {
                string extraText = "Could be due to 'new FilteredElementCollector(...)'";
                MessageBox.Show(
                    string.Format("Message:\n {0}\n{1}", ex.Message, extraText),
                    "Error: " + ex.ToString());
            }
            catch (ArgumentException ex)
            {
                string extraText = "Could be due to 'new FilteredElementCollector(...)'";
                MessageBox.Show(
                    string.Format("Message:\n {0}\n{1}", ex.Message, extraText),
                    "Error: " + ex.ToString());
            }
            catch (InvalidOperationException ex)
            {
                string extraText = "Could be due to 'collector.ToElements()'";
                MessageBox.Show(
                    string.Format("Message:\n {0}\n{1}", ex.Message, extraText),
                    "Error: " + ex.ToString());
            }
            catch (Exception ex)
            {
                string extraText = "Unknown";
                MessageBox.Show(
                    string.Format("Message:\n {0}\n{1}", ex.Message, extraText),
                    "Error: " + ex.ToString());
            }

            return output;
        }

        public List<ElementId> GetElementIdsFromSelection()
        {
            List<ElementId> output = null;

            ICollection<ElementId> currentSelected = this.uiDoc.Selection.GetElementIds();

            if (currentSelected != null)
            {
                output = currentSelected.ToList<ElementId>();
            }

            return output;
        }

        #endregion ElementId Getters

        #region View Related Tasks

        /// <summary>
        /// Updates View 'this.view' with a new view that is of
        /// ViewType 'type' from the Document 'this.doc'
        /// unless special settings are enforced.
        /// </summary>
        /// <param name="type">If newView.ViewType is equal to type, then result is Valid and vice versa</param>
        /// <param name="force">If true, sets this.view to the latest view of this.doc.ActiveView whether or not its valid</param>
        /// <returns></returns>
        public int UpdateView(bool force = false)
        {
            List<ViewType> types = new List<ViewType>()
            {
                ViewType.EngineeringPlan,
                ViewType.FloorPlan,
                ViewType.CeilingPlan,
                ViewType.ThreeD,
                ViewType.Elevation
            };

            // Get doc.ActiveView
            View newView = this.doc.ActiveView;
            int result = 0;

            // Check if the result given is valid or not
            if (newView == null)
            {
                // Means, newView changed to an invalid type
                result = -2;
            }
            else if (!types.Contains(newView.ViewType))
            {
                // Means, newView changed to a different type (could still be applicable)
                result = -1;
            }
            else if (this.view != null)
            {
                if (this.view.Id == newView.Id)
                {
                    // Means, newView hasn't changed meaningfully
                    result = 0;
                }
                else
                {
                    // Means, newView has changed to a valid and meaningful value
                    result = 1;
                }
            }
            else
            {
                // Means, newView has changed to a valid and meaningful value
                result = 1;
            }

            // If its forced or the result is valid, then set newView to this.View
            if (force || (result == 1))
            {
                this.view = newView;
            }

            return result;
        }

        #endregion View Related Tasks

        #region Selection Related Tasks

        public void MakeNewSelection(List<ElementId> selection)
        {
            this.uiDoc.Selection.SetElementIds(selection);
        }

        public void IsSelectionChanged(List<ElementId> selection)
        {
            ICollection<ElementId> elementIds = this.uiDoc.Selection.GetElementIds();

            elementIds.Equals(selection);

        }

        #endregion Selection Related Tasks

        #region Filter ElementIds

        /// <summary>
        /// Filter the movable elementIds by a whitelist of movable category names
        /// </summary>
        /// <param name="elementIds"></param>
        /// <returns></returns>
        public List<ElementId> GetMovableElementIds(List<ElementId> elementIds)
        {
            HashSet<string> movableCategories = new HashSet<string>()
            {
                "Columns",
                "Structural Columns",
                "Structural Connections",
                "Structural Framing",
                "Generic Models",
                "Stacked Walls",
                "Walls",
                "Floors"
            };

            List<ElementId> output = FilterByCategory(elementIds, movableCategories);

            return output;
        }

        /// <summary>
        /// Get the elementIds that have the same category names as the set of categoryNames
        /// </summary>
        /// <param name="elementIds"></param>
        /// <param name="categoryNames"></param>
        /// <returns></returns>
        public List<ElementId> FilterByCategory(List<ElementId> elementIds, HashSet<string> categoryNames)
        {
            // Get elements from the set of elementIds
            IEnumerable<Element> elements
                = from ElementId id in elementIds
                  select this.GetElement(id);
            // Get elementIds from the elements that has the same category name as one of the given categoryNames
            IEnumerable<ElementId> elementIdsWithCategoryNames
                = from Element e in elements
                    where categoryNames.Contains(this.GetCategory(e).Name)
                    select e.Id;
            return elementIdsWithCategoryNames.ToList();
        }

        #endregion Filter ElementIds

        #region Movement / Shift Related Tasks

        /// <summary>
        /// Moves the selected elements with an option to copy them as well
        /// </summary>
        /// <param name="elementIds"></param>
        /// <param name="displacement"></param>
        /// <param name="copy"></param>
        /// <returns></returns>
        public List<ElementId> CopyAndShiftElements(
            List<ElementId> elementIds,
            List<int> displacement,
            bool copy = false
            )
        {
            // Check for any arguments that is null
            if (elementIds == null)
                throw new ArgumentNullException("elementIds");
            else if (displacement == null)
                throw new ArgumentNullException("displacement");
            else if (displacement.Count != 3)
                throw new ArgumentException(String.Format("displacement has {0} items, must have 3 (x, y, z)", displacement.Count));

            // If elementIds is empty, then return immediately with no elements affected by this method
            if (elementIds.Count == 0) return new List<ElementId>();

            // Get a full list of elements from elementIds
            IEnumerable<Element> elements
                = from ElementId id in elementIds
                  select this.GetElement(id);
            // If elements is somehow null, then throw an exception
            if (elements == null) throw new NullReferenceException("elements");

            // Call back-end function to perform the operation
            HashSet<ElementId> affected = DisplaceElements(elements, displacement, copy);

            // Return affected in list form
            return affected.ToList();
        }

        /// <summary>
        /// Back-end facing method that displaces the elements by the given displacement values with an option to copy
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="displacement"></param>
        /// <param name="copy"></param>
        /// <returns></returns>
        private HashSet<ElementId> DisplaceElements(
            IEnumerable<Element> elements,
            List<int> displacement,
            bool copy = false
            )
        {
            // Get a hashset to keep track of elementIds that are either affected by the shift, or newly created elements from a copy function
            HashSet<ElementId> affected = new HashSet<ElementId>();

            Location loc;
            HashSet<ElementId> subAffected;
            foreach (Element elem in elements)
            {
                // If the element's location exists, set loc to that location, if not, iterate to the next element
                if (elem.Location != null) { loc = elem.Location; } else { continue; }

                // Get the elements that are affected from displacing that element
                // (in case that more than one elements are newly created, thus affected)
                subAffected = DisplaceElement(elem, displacement, loc, copy);

                // Add the elements of subAffected onto affected
                affected.UnionWith(subAffected);
            }

            return affected;
        }

        /// <summary>
        /// Back-end facing method that displaces one element by the given displacement values with an option to copy
        /// </summary>
        /// <param name="element"></param>
        /// <param name="displacement"></param>
        /// <param name="location"></param>
        /// <param name="copy"></param>
        /// <returns></returns>
        private HashSet<ElementId> DisplaceElement(
            Element element,
            List<int> displacement,
            Location location,
            bool copy = false
            )
        {
            HashSet<ElementId> affected = new HashSet<ElementId>();

            // Get x, y, and z value (in feet and inches)
            double xValue = ConvertMM2FeetInch(displacement[0]);
            double yValue = ConvertMM2FeetInch(displacement[1]);
            double zValue = ConvertMM2FeetInch(displacement[2]);

            // Generate object to modify the x and y value
            XYZ newXY = new XYZ(xValue, yValue, 0);

            // Get z parameters to modify the z value
            List<string> zParams = this.GetZParamFromLoc(location);

            // if method is copying, then tranName = "Copy and Move Elements", else, "Move Elements"
            string tranName = copy ? "Copy and Move Elements" : "Move Elements";
            // set transSuccess to true, used to determine whether or not to roll back the changes made to this element
            bool tranSuccess = true;

            using (Transaction tran = new Transaction(this.doc, tranName))
            {
                tran.Start();

                if (copy)
                {
                    // If copied, then gather the elements that are newly created from the copy and add it into affected
                    ICollection<ElementId> elementsCopied = ElementTransformUtils.CopyElement(this.doc, element.Id, newXY);
                    affected.UnionWith(elementsCopied);
                }
                else
                {
                    // If not copied, then get the current element's id and add it into affected
                    ElementTransformUtils.MoveElement(this.doc, element.Id, newXY);
                    affected.Add(element.Id);
                }

                List<Parameter> parameters;
                foreach (ElementId id in affected)
                {
                    // Get the parameter for Z value
                    parameters = GetParameters(this.GetElement(id), zParams);

                    double elevationDouble;
                    string elevationString;
                    foreach (Parameter p in parameters)
                    {
                        // Get the the new elevation value for the given parameter
                        elevationDouble = ConvertStringToFeetInch(p.AsValueString()) + zValue;
                        elevationString = ConvertFeetInchToString(elevationDouble);
                        // Apply the elevation value into the parameter
                        p.SetValueString(elevationString);
                    }
                }

                // If the transation is a success, commit the changes, else, roll back (undo) the changes made
                if (tranSuccess) { tran.Commit(); } else { tran.RollBack(); }
            }

            return affected;
        }

        /// <summary>
        /// Given the location, get the list of parameter names that have to do with modifying the z values
        /// </summary>
        /// <param name="loc"></param>
        /// <returns></returns>
        private List<string> GetZParamFromLoc(Location loc)
        {
            List<string> zParams;

            LocationPoint point = loc as LocationPoint;
            LocationCurve curve = loc as LocationCurve; 

            if (point != null)
            {
                // z params for location point type elements
                zParams = new List<string>()
                {
                    "Top Offset",
                    "Base Offset",
                    "Label Elevation",
                    "Offset"
                };
            }
            else if (curve != null)
            {
                // z params for location curve type elements
                zParams = new List<string>()
                {
                    "Base Offset",
                    "Top Offset",
                    "Start Level Offset",
                    "End Level Offset"
                };
            }
            else
            {
                // z params for null location type elements
                zParams = new List<string>()
                {
                    "Height Offset From Level"
                };
            }

            return zParams;
        }

        /// <summary>
        /// Get the list of parameters given the list of parameter names
        /// </summary>
        /// <param name="element"></param>
        /// <param name="paramNames"></param>
        /// <returns></returns>
        private List<Parameter> GetParameters(Element element, List<string> paramNames)
        {
            // Initialize the list of parameters to be returned
            List<Parameter> output = new List<Parameter>();

            foreach (string names in paramNames)
            {
                // Get the parameters from the element
                IList<Parameter> parameters = element.GetParameters(names);

                // Conditions to iterate to the next name immediately
                if (parameters == null)
                    continue;
                else if (parameters.Count == 0)
                    continue;
                else if (parameters[0].IsReadOnly)
                    continue;

                // Add to list of parameters
                output.Add(parameters[0]);
            }

            return output;
        }

        #region Unit Conversions

        private double ConvertMM2FeetInch(double millimeters)
        {
            double mmToFeetInch = UnitUtils.Convert(
                millimeters,
                DisplayUnitType.DUT_MILLIMETERS,
                DisplayUnitType.DUT_FEET_FRACTIONAL_INCHES);

            return mmToFeetInch;
        }

        private double ConvertFeetInch2MM(double feetInch)
        {
            double feetInchToMM = UnitUtils.Convert(
                feetInch,
                DisplayUnitType.DUT_FEET_FRACTIONAL_INCHES,
                DisplayUnitType.DUT_MILLIMETERS);

            return feetInchToMM;
        }

        private double ConvertStringToFeetInch(string feetInchString)
        {
            double feetInch = 0;

            Units units = this.doc.GetUnits();
            if (!UnitFormatUtils.TryParse(units, UnitType.UT_Length, feetInchString, out feetInch))
            {
                throw new FormatException();
            }

            return feetInch;
        }

        private string ConvertFeetInchToString(double feetInch)
        {
            Units units = this.doc.GetUnits();
            string output = UnitFormatUtils.Format(units, UnitType.UT_Length, feetInch, true, true);

            return output;
        }

        private string ConvertMM2FeetInchString(double millimeters)
        {
            double feetInch = ConvertMM2FeetInch(millimeters);
            string output = ConvertFeetInchToString(feetInch);

            return output;
        }

        #endregion Unit Conversions

        #endregion Movement / Shift Related Tasks

        #region Get ElementId Grouping

        public SortedDictionary<string, List<ElementId>> GroupElementIdsBy(Type type, List<ElementId> elementIds)
        {
            string GetKey(Type t, ElementId eId)
            {
                // Based on the type and the nullity of the object recieved, return an output
                string tmpKey = "null";

                // If Type t isn't null, then test t against the given types and nullity
                // to set the value of tmpKey based on those results
                if (t != null)
                {
                    if (t == "CategoryType".GetType())
                    {
                        tmpKey = this.GetCategoryType(this.GetCategory(this.GetElement(eId))).ToString();
                    }
                    else if (t == typeof(Category))
                    {
                        Category cat = this.GetCategory(this.GetElement(eId));
                        if (cat != null)
                            tmpKey = cat.Name;
                        else
                            tmpKey = "No Category";
                    }
                    else if (t == typeof(Family))
                    {
                        tmpKey = this.GetFamilyName(this.GetElementType(this.GetElement(eId)));
                        if (tmpKey == "")
                            tmpKey = "No Family";
                    }
                    else if (t == typeof(ElementType))
                    {
                        ElementType eType = this.GetElementType(this.GetElement(eId));
                        if (eType != null)
                            tmpKey = eType.Name;
                        else
                            tmpKey = "No Type";
                    }
                    else if (t == typeof(ElementId))
                    {
                        Element element = this.GetElement(eId);
                        if (element != null)
                            tmpKey = element.UniqueId;
                        else
                            tmpKey = "None";
                    }
                    else
                    {
                        tmpKey = "null";
                    }
                }

                return tmpKey;
            }

            SortedDictionary<string, List<ElementId>> output = new SortedDictionary<string, List<ElementId>>();
            string key = string.Empty;

            foreach(ElementId eId in elementIds)
            {
                // get an object based on type
                key = GetKey(type, eId);

                // If the output doesn't have a category for elementCatType, make a new one
                if (!output.ContainsKey(key))
                    output.Add(key, new List<ElementId>());

                // Add eId to the elementCatType
                output[key].Add(eId);
            }

            return output;
        }

        public Dictionary<string, List<ElementId>> GroupElementIdsByCategoryType(List<ElementId> elementIds)
        {
            Dictionary<string, List<ElementId>> output = new Dictionary<string, List<ElementId>>();
            CategoryType elementCatType = CategoryType.Invalid;
            string key = null;

            foreach (ElementId eId in elementIds)
            {
                // Get element Category Type
                elementCatType = this.GetCategoryType(this.GetCategory(this.GetElement(eId)));

                // Turn the object to a string key
                key = elementCatType.ToString();

                // If the output doesn't have a category for elementCatType, make a new one
                if (!output.ContainsKey(key))
                    output.Add(key, new List<ElementId>());

                // Add eId to the elementCatType
                output[key].Add(eId);
            }

            return output;
        }

        public Dictionary<string, List<ElementId>> GroupElementIdsByCategories(List<ElementId> elementIds)
        {
            Dictionary<string, List<ElementId>> output = new Dictionary<string, List<ElementId>>();
            Category elementCat = null;
            string key = null;

            foreach (ElementId eId in elementIds)
            {
                // Get element Category Type
                elementCat = this.GetCategory(this.GetElement(eId));

                // Turn the object into a string key
                if (elementCat == null)
                    key = "No Category";
                else
                    key = elementCat.ToString();

                // If the output doesn't have a category for elementCatType, make a new one
                if (!output.ContainsKey(key))
                    output.Add(key, new List<ElementId>());

                // Add eId to the elementCatType
                output[key].Add(eId);
            }

            return output;
        }

        // public Dictionary<string, List<ElementId>> 

        public List<CategoryType> GetAllCategoryTypes()
        {
            List<CategoryType> output = new List<CategoryType>();
            Categories categories = this.doc.Settings.Categories;

            // Add in Invalid just in case
            output.Add(CategoryType.Invalid);
            // Construct a list of unique categoryType from categories
            foreach (Category c in categories)
            {
                if (output.Contains(c.CategoryType))
                    continue;
                output.Add(c.CategoryType);
            }

            return output;
        }


        #endregion Get ElementId Grouping

        #region Get Element Information

        public CategoryType GetCategoryType(Category category)
        {
            CategoryType categoryType = CategoryType.Invalid;

            if (category == null)
                return categoryType;

            categoryType = category.CategoryType;

            return categoryType;
        }

        public Category GetCategory(Element element)
        {
            Category category = null;

            if (element == null)
                return category;

            category = element.Category;

            return category;
        }

        public string GetFamilyName(ElementType elementType)
        {
            string familyName = "";

            if (elementType == null)
                return familyName;

            familyName = elementType.FamilyName;

            return familyName;
        }
        
        public ElementType GetElementType(Element element)
        {
            ElementType type = null;

            ElementId typeId = element.GetTypeId();

            if (typeId == null)
                return type;

            type = this.doc.GetElement(typeId) as ElementType;

            return type;
        }

        public Element GetElement(ElementId eId)
        {
            Element element = null;

            if (eId == null) return element;

            element = this.doc.GetElement(eId);

            return element;
        }

        #endregion Get ElementInformation

        #region Element Visibility

        public void HideUnselectedElementIds(
            List<ElementId> selection,
            List<ElementId> allElements)
        {
            View view = this.View;
            List<ElementId> hideIds = new List<ElementId>();
            List<ElementId> showIds = new List<ElementId>();

            // Construct hideIds and showIds
            foreach (ElementId id in allElements)
            {
                Element e = this.doc.GetElement(id);
                if ((e as View) == null)
                {
                    if (!selection.Contains(id))
                    {
                        if (e.CanBeHidden(view) && (!e.IsHidden(view)))
                        {
                            hideIds.Add(id);
                        }
                    }
                    else
                    {
                        if (e.IsHidden(view))
                        {
                            showIds.Add(id);
                        }
                    }
                }
            }

            using (Transaction tran = new Transaction(this.doc, "Test"))
            {
                tran.Start();

                if (view != null)
                {
                    if (hideIds.Count != 0)
                        view.HideElements(hideIds);
                    if (selection.Count != 0)
                        view.UnhideElements(selection);
                }

                tran.Commit();
            }

        }

        public void ShowSelectedElementIds(
            List<ElementId> selection)
        {
            View view = this.View;
            List<ElementId> showIds = new List<ElementId>();

            foreach (ElementId id in selection)
            {
                Element e = this.doc.GetElement(id);
                if (e == null) continue;

                if (e.IsHidden(view))
                {
                    showIds.Add(id);
                }
            }

            if (showIds.Count == 0) return;

            using (Transaction tran = new Transaction(this.doc, "Show Elements"))
            {
                tran.Start();

                if (view != null)
                {
                    view.UnhideElements(showIds);
                }

                tran.Commit();
            }

        }

        public void HideElementIds(HashSet<ElementId> elementIds, TreeStructure tree)
        {
            View view = this.View;

            if (view == null) return;

            // Get all the elementIds to hide
            IEnumerable<Element> elements
                = from ElementId id in elementIds
                    select this.doc.GetElement(id);
            IEnumerable<ElementId> elementIdsToHide
                = from Element e in elements
                  where (e as View == null)
                  && (e.CanBeHidden(view))
                  && (!e.IsHidden(view))
                  select e.Id;

            if (elementIdsToHide.Count<ElementId>() == 0) return;

            // Update treeStructure of what the current hidden elementIds are
            foreach (ElementId id in elementIdsToHide)
            {
                if (!tree.HiddenNodes.ContainsKey(view.Id))
                {
                    tree.HiddenNodes.Add(view.Id, new HashSet<ElementId>());
                }
                tree.HiddenNodes[view.Id].Add(id);
            }

            using (Transaction tran = new Transaction(this.doc, "Hide Elements"))
            {
                tran.Start();

                view.HideElements(elementIdsToHide.ToList());

                tran.Commit();
            }
        }

        public void ShowElementIds(HashSet<ElementId> elementIds, TreeStructure tree)
        {
            View view = this.View;

            if (view == null) return;

            // Get all the elementIds to show
            IEnumerable<Element> elements
                = from ElementId id in elementIds
                  select this.doc.GetElement(id);
            IEnumerable<ElementId> elementIdsToShow
                = from Element e in elements
                  where e.IsHidden(view)
                  select e.Id;

            if (elementIdsToShow.Count<ElementId>() == 0) return;

            // Update treeStructure of what the current hidden elementIds are
            foreach (ElementId id in elementIdsToShow)
            {
                if (!tree.HiddenNodes.ContainsKey(view.Id))
                    continue;

                tree.HiddenNodes[view.Id].Remove(id);

                if (tree.HiddenNodes[view.Id].Count == 0)
                    tree.HiddenNodes.Remove(view.Id);
            }

            using (Transaction tran = new Transaction(this.doc, "Show Elements"))
            {
                tran.Start();

                view.UnhideElements(elementIdsToShow.ToList());

                tran.Commit();
            }

        }

        #endregion Element Visibility

        #endregion Public Methods

    }
}
