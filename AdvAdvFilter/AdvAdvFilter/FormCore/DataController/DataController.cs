﻿namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    using TreeNode = System.Windows.Forms.TreeNode;
    using TreeNodeCollection = System.Windows.Forms.TreeNodeCollection;

    using FilterMode = AdvAdvFilter.Common.FilterMode;

    /// <summary>
    /// DataController acts as a layer between the ModelessForm and the Revit Software
    ///     to make it more convenient to update and retrieve data, and provides
    ///     information if the update has made any significant change to the data.
    /// </summary>
    public class DataController
    {

        #region DataTypes
        /*
        public enum FilterMode
        {
            Selection = 0,
            View = 1,
            Project = 2,
            Custom = 3,
            Invalid = -1
        }
        */

        #endregion DataTypes

        #region Fields

        private List<ElementId> allElements;
        private List<ElementId> selElements;

        // Fields related to movement
        private List<ElementId> movElements;
        private HashSet<ElementId> idsToHide;
        private HashSet<ElementId> idsToMove;
        private bool copyAndShift;
        private List<int> coords;
        // 
        private TreeStructure elementTree;

        private readonly object padLock = new object();

        #endregion Fields

        #region Parameters

        public View View
        {
            get
            {
                lock (padLock) { return this.elementTree.Doc.ActiveView; }
            }
        }

        public HashSet<ElementId> AllElements
        {
            get
            {
                lock (padLock) { return this.elementTree.SubSet; }
            }
        }

        public HashSet<ElementId> SelElementIds
        {
            get
            {
                lock (padLock) { return this.elementTree.SelectedNodes; }
            }
        }

        public HashSet<ElementId> IdsToHide
        {
            get { return this.idsToHide; }
            set
            {
                if (value == null)
                    this.idsToHide.Clear();
                else
                    this.idsToHide = value;
            }
        }

        public HashSet<ElementId> IdsToMove
        {
            get { return this.idsToMove; }
            set
            {
                if (value == null)
                    this.idsToMove.Clear();
                else
                    this.idsToMove = value;
            }
        }

        public List<ElementId> MovElements
        {
            set
            {
                if (value == null)
                    this.movElements.Clear();
                else if (value.Count == 0)
                    this.movElements.Clear();
                else
                    this.movElements = value;
            }
            get
            {
                if (this.movElements == null)
                    this.movElements = new List<ElementId>();
                return this.movElements;
            }
        }

        public bool CopyAndShift
        {
            get { return this.copyAndShift; }
            set { this.copyAndShift = value; }
        }

        public List<int> Coords
        {
            get
            {
                List<int> output;
                if (this.coords == null)
                {
                    output = new List<int>() { 0, 0, 0 };
                }
                else if (this.coords.Count != 3)
                {
                    output = new List<int>() { 0, 0, 0 };
                }
                else
                {
                    output = this.coords;
                }
                return output;
            }
            set
            {
                if (value == null)
                {
                    this.coords.Clear();
                }
                else if (value.Count != 3)
                {
                    throw new ArgumentException();
                }
                else
                {
                    this.coords = value;
                }                        
            }
        }

        public TreeStructure ElementTree
        {
            get
            {
                lock (padLock) { return this.elementTree; }
            }
        }

        #endregion Parameters

        #region Constructor

        public DataController(Document doc)
        {
            if (doc == null) throw new ArgumentNullException();

            this.allElements = new List<ElementId>();
            this.selElements = new List<ElementId>();
            // Fields related to movement
            this.movElements = new List<ElementId>();

            this.idsToHide = new HashSet<ElementId>();

            this.copyAndShift = true;
            this.coords = new List<int>() { 0, 0, 0 };

            this.elementTree = new TreeStructure(doc);
        }

        #endregion Constructor

        #region Controls

        public bool SetMode(FilterMode mode, bool force = false)
        {
            return this.elementTree.SetSubSet(mode, force);
        }

        #endregion Controls

        #region AllElements Operations

        public void SetAllElements(List<ElementId> elementIds)
        {
            lock (padLock)
            {
                this.elementTree.ClearAll();
                this.elementTree.AppendList(elementIds);
            }
        }

        public void ClearAllElements()
        {
            lock (padLock)
            {
                this.elementTree.ClearAll();
            }
        }

        public void AddToAllElements(List<ElementId> elementIds)
        {
            lock (padLock)
            {
                this.elementTree.AppendList(elementIds);
            }
        }

        public void RemoveFromAllElements(List<ElementId> elementIds)
        {
            lock (padLock)
            {
                this.elementTree.RemoveList(elementIds);
            }
        }

        #endregion AllElements Operations

        #region ElementId Getters

        public HashSet<ElementId> GetElementIdsByPath(List<string> path)
        {
            lock (padLock)
            {
                return elementTree.GetElementIdsByPath(path);
            }
        }

        #endregion ElementId Getters

        #region Auxiliary Methods

        /// <summary>
        /// Updates the elements of this.allElements with newAllElements
        /// </summary>
        /// <param name="newAllElements"></param>
        /// <returns>true if this.allElements != newAllElements, else false</returns>
        public bool UpdateAllElements(List<ElementId> newAllElements)
        {
            bool listChanged = false;

            // Perform tests to see if the list has been changed
            if ((newAllElements == null) && (this.allElements == null))
            {
                listChanged = false; // If both are null, then they didn't change
            }
            else if (((newAllElements == null) && (this.allElements != null))
                || ((newAllElements != null) && (this.allElements == null)))
            {
                listChanged = true; // If one of them is null and the other not, then it changed
            }
            else
            {
                // Perform a LINQ? statement
                // listChanged = (!newAllElements.All(this.allElements.Contains));
                listChanged = (!newAllElements.SequenceEqual(this.allElements));
            }

            // If listChanged, then...
            if (listChanged)
            {
                if (newAllElements != null)
                {
                    // If newAllElements isn't null, then update this.allElements
                    this.allElements = newAllElements;
                }
                else
                {
                    // If its null, then simply clear all items from this.allElements
                    this.allElements.Clear();
                }
            }

            return listChanged;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newSelElements"></param>
        /// <returns></returns>
        public bool UpdateSelElements(List<ElementId> newSelElements)
        {
            // Check if the list has changed by doing something of a LINQ? statement
            bool listChanged = (!newSelElements.All(this.selElements.Contains));

            // If the list has detected a change, then update this.selElements
            if (listChanged)
                this.selElements = newSelElements;

            return listChanged;
        }

        /// <summary>
        /// Checks if two lists of ElementId have the same contents (ignore ordering)
        /// </summary>
        /// <param name="list1"></param>
        /// <param name="list2"></param>
        /// <returns></returns>
        public bool IsListEqual(List<ElementId> list1, List<ElementId> list2)
        {
            bool b1 = list1.All(e => list2.Contains(e));
            bool b2 = list2.All(e => list1.Contains(e));
            bool b3 = (list1.Count == list2.Count);

            bool result = b1 && b2 && b3;

            return result;
        }

        /// <summary>
        /// Returns true if the argument's HashSet is different from this.SelElementIds' HashSet, else false
        /// </summary>
        /// <param name="revitSelection"></param>
        /// <returns></returns>
        public bool DidSelectionChange(HashSet<ElementId> revitSelection)
        {
            // Check if they are the same length
            bool sameLength = (this.SelElementIds.Count == revitSelection.Count);
            // If not, then prematurely return true
            if (!sameLength) return true;
            // Check if one is the superset of another AFTER confirming equal lengths
            bool sameItems = this.SelElementIds.IsSupersetOf(revitSelection);
            // If one is not a superset of another when they have the same length, then they are not the same
            return !sameItems;
        }

        #endregion Auxiliary Methods


    }

}
