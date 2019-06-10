﻿namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using Autodesk.Revit.DB;

    using FilterMode = AdvAdvFilter.Common.FilterMode;

    public class TreeStructure
    {
        public enum depth
        {
            CategoryType = 0,
            Category = 1,
            Family = 2,
            ElementType = 3,
            Instance = 4,
            Invalid = -1
        };

        #region Fields

        private Dictionary<ElementId, TreeNode> elementIdNodes;
        private Dictionary<ElementId, HashSet<ElementId>> viewElementId;
        private ElementSet setTree;
        private Document doc;

        private FilterMode currentMode;

        private HashSet<ElementId> subSet;

        public HashSet<ElementId> VisibleNodes { get; set; }
        public HashSet<ElementId> SelectedNodes { get; set; }

        /*
        private Dictionary<ElementId, HashSet<ElementId>> elementIdsInView;
        private HashSet<ElementId> customElementIds;
        */
        #endregion Fields

        #region Parameters

        public Dictionary<ElementId, TreeNode> ElementIdNodes
        { get { return this.elementIdNodes; } }

        public ElementSet SetTree
        { get { return this.setTree; } }

        public Document Doc
        { get { return this.doc; } }

        public HashSet<ElementId> SubSet
        {
            get { return this.subSet; }
        }

        #endregion Parameters

        public TreeStructure(Document doc)
        {
            this.doc = doc;

            this.elementIdNodes = new Dictionary<ElementId, TreeNode>();

            this.viewElementId = new Dictionary<ElementId, HashSet<ElementId>>();

            this.setTree = new ElementSet();
            this.setTree.Name = "All";

            this.currentMode = FilterMode.Invalid;

            this.subSet = new HashSet<ElementId>();

            this.VisibleNodes = new HashSet<ElementId>();
            this.SelectedNodes = new HashSet<ElementId>();
        }

        #region ElementTree Operations

        #region Clear and reset

        public void ClearAll()
        {
            this.elementIdNodes.Clear();

            this.viewElementId.Clear();

            this.setTree.Branch.Clear();
            this.setTree.Set.Clear();

            this.VisibleNodes.Clear();

            this.SelectedNodes.Clear();
        }

        #endregion Clear and reset

        #region Add Nodes To Tree

        public void AppendList(List<ElementId> elementIds)
        {
            List<NodeData> nodesToAdd = new List<NodeData>();

            // Add to elementIdNodes first
            foreach (ElementId id in elementIds)
            {
                if (this.elementIdNodes.ContainsKey(id)) continue;

                // Generate NodeData to be put into node
                NodeData data = GenerateNodeData(id, this.doc);
                // Generate the TreeNode that uses the information of NodeData to initialize
                TreeNode node = GenerateTreeNode(data);

                // Add elementId to listOfElementIds to be added in the tree
                nodesToAdd.Add(data);

                // Add elementid and its corresponding node in this.elementIdNodes
                this.elementIdNodes.Add(id, node);
                // If viewElementId doesn't contain any entries corresponding to data.OwnerViewId, then create one
                if (!this.viewElementId.ContainsKey(data.OwnerViewId))
                    this.viewElementId.Add(data.OwnerViewId, new HashSet<ElementId>());                
                this.viewElementId[data.OwnerViewId].Add(id);
            }

            // Update the internal tree structure
            AddToTree(nodesToAdd, this.setTree, depth.CategoryType);
        }

        private void AddToTree(List<NodeData> nodes, ElementSet set, depth depth)
        {
            depth newDepth;
            Dictionary<string, List<NodeData>> grouping;

            // Retrieve the next depth            
            if (depth == depth.Invalid)
            {
                // If current depth is invalid, then throw an exception, the invalid depth indicates that something has gone wrong.
                throw new ArgumentException();
            }
            else if (depth == depth.Instance)
            {
                // current depth is the last depth, set nextDepth to depth.Invalid
                newDepth = depth.Invalid;

                foreach (NodeData data in nodes)
                    set.Set.Add(data.Id);
                return;
            }
            else
            {
                // Get next depth down
                newDepth = (depth)((int)depth + 1);
            }

            // Get the grouping on a paramName for all the nodes (CategoryType, Category, Family, ElementType)
            grouping = GetGroupingByNodeData(nodes, depth.ToString());

            // For each KeyValuePair in grouping...
            ElementSet newSet;
            foreach (KeyValuePair<string, List<NodeData>> kvp in grouping)
            {
                // Get a new ElementSet corresponding to kvp.Key
                newSet = set.GetElementSet(kvp.Key);

                // If newSet doesn't exist, then add a branch and set newSet to the newly created branch
                if (newSet == null)
                {
                    newSet = set.AddBranch(kvp.Key);
                    set.Name = depth.ToString();
                }

                //----- Debug ------
                /*
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(depth.ToString());
                sb.AppendLine(kvp.Key);
                foreach (NodeData d in kvp.Value)
                {
                    sb.AppendLine(d.Id.ToString());
                }
                MessageBox.Show(sb.ToString());
                */
                //----- Debug ------

                // Recurse down
                AddToTree(kvp.Value, newSet, newDepth);

                // When returning from the recursion, add the newSet's set
                // onto set.Set, as it should be recently updated 
                set.Set.UnionWith(newSet.Set);
            }
        }

        #endregion Add Nodes To Tree

        #region Remove Nodes From Tree

        public void RemoveList(List<ElementId> elementIds)
        {
            List<NodeData> nodesToRemove = new List<NodeData>();

            // Add to elemenetIdNodes first
            foreach (ElementId id in elementIds)
            {
                if (!this.elementIdNodes.ContainsKey(id)) continue;

                // Retrieve the TreeNode corresponding to elementId
                TreeNode node = this.elementIdNodes[id];
                // Retrieve NodeData that is 'tagged' onto the node
                NodeData data = node.Tag as NodeData;
                // If data is null, then something is terribly wrong, throw an exception
                if (data == null) throw new NullReferenceException();

                // Add 'data' onto the list of nodes to remove
                nodesToRemove.Add(data);

                // Remove the entry corresponding to 'id' in this.elementIdNodes
                this.elementIdNodes.Remove(id);
                this.viewElementId[data.OwnerViewId].Remove(id);
                // If the number of entries corresponding to data.OwnerViewId is 0, then remove that dictionary key
                if (this.viewElementId[data.OwnerViewId].Count == 0)
                {
                    this.viewElementId.Remove(data.OwnerViewId);
                }
            }

            // Remove the all occurances of the ElementIds in nodesToRemove from the tree
            RemoveFromTree(nodesToRemove, this.setTree, depth.CategoryType);

            List<string> branchesToRemove = new List<string>();
            foreach (KeyValuePair<string, ElementSet> kvp in this.setTree.Branch)
            {
                if (kvp.Value.Set.Count == 0)
                    branchesToRemove.Add(kvp.Key);
            }

            foreach (string key in branchesToRemove)
                this.setTree.RemoveBranch(key);
        }

        private void RemoveFromTree(List<NodeData> nodes, ElementSet set, depth depth)
        {
            depth newDepth;
            Dictionary<string, List<NodeData>> grouping;

            // Retrieve the next depth            
            if (depth == depth.Invalid)
            {
                // If current depth is invalid, then throw an exception, the invalid depth indicates that something has gone wrong.
                throw new ArgumentException();
            }
            else if (depth == depth.Instance)
            {
                // current depth is the last depth, set nextDepth to depth.Invalid
                newDepth = depth.Invalid;

                foreach (NodeData data in nodes)
                    set.Set.Remove(data.Id);
                return;
            }
            else
            {
                // Get next depth down
                newDepth = (depth)((int)depth + 1);
            }

            // Get the grouping on a paramName for all the nodes (CategoryType, Category, Family, ElementType)
            grouping = GetGroupingByNodeData(nodes, depth.ToString());

            // For each KeyValuePair in grouping...
            ElementSet newSet;
            foreach (KeyValuePair<string, List<NodeData>> kvp in grouping)
            {
                // Get a new ElementSet corresponding to kvp.Key
                newSet = set.GetElementSet(kvp.Key);

                // If newSet doesn't exist, then add a branch and set newSet to the newly created branch
                if (newSet == null)
                    throw new NullReferenceException();

                //----- Debug ------
                /*
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(depth.ToString());
                sb.AppendLine(kvp.Key);
                foreach (NodeData d in kvp.Value)
                {
                    sb.AppendLine(d.Id.ToString());
                }
                MessageBox.Show(sb.ToString());
                */
                //----- Debug ------

                // Recurse down
                RemoveFromTree(kvp.Value, newSet, newDepth);

                if (newSet.Set.Count == 0)
                {
                    // When returning from the recursion, if the branch's contents are empty,
                    // then remove the branch from the current ElementSet
                    set.RemoveBranch(kvp.Key);
                }

                // When returning from the recursion,
                // remove the elementIds found within kvp.Value              
                foreach (NodeData data in kvp.Value)
                    set.Set.Remove(data.Id);
            }
        }

        #endregion Remove Nodes From Tree

        #endregion ElementTree Operations

        #region SubSet Operations

        public void SetSubSet (FilterMode mode)
        {
            if (this.currentMode == mode) return;

            // FilteredElementCollector collector = new FilteredElementCollector(this.doc, this.setTree.Set);

            switch (mode)
            {
                case FilterMode.Project:
                    this.subSet = this.setTree.Set;
                    break;
                case FilterMode.View:
                    ElementId viewId = this.doc.ActiveView.Id;
                    if (this.viewElementId.ContainsKey(viewId))
                        this.subSet = this.viewElementId[viewId];
                    else
                        this.subSet.Clear();
                    break;
                case FilterMode.Selection:
                    this.subSet = this.SelectedNodes;
                    break;
                default:
                    break;
            }
            
            this.currentMode = mode;
        }

        #endregion SubSet Operations

        #region Auxiliary Functions

        private Dictionary<string, List<NodeData>> GetGroupingByNodeData(List<NodeData> nodes, string paramName)
        {
            Dictionary<string, List<NodeData>> grouping = new Dictionary<string, List<NodeData>>();

            string key;
            foreach (NodeData data in nodes)
            {
                key = data.GetParameter(paramName);

                if (!grouping.ContainsKey(key))
                    grouping.Add(key, new List<NodeData>());

                grouping[key].Add(data);
            }

            return grouping;
        }

        public TreeNode GenerateTreeNode(NodeData data)
        {
            if (data == null) throw new ArgumentNullException();

            TreeNode node = new TreeNode();
            node.Name = data.Id.ToString();
            node.Text = node.Name;
            node.Tag = data;

            return node;
        }

        private NodeData GenerateNodeData(ElementId elementId, Document doc)
        {
            // Fail the execution if GenerateNodeData has elementId or doc as null
            if ((elementId == null) || (doc == null))
                throw new ArgumentNullException();

            // Get elementId's element
            Element element = doc.GetElement(elementId);

            // If resulting element is null, fail the process, as it should always return not null
            if (element == null) throw new InvalidOperationException();

            // Generate NodeData
            NodeData data = new NodeData();

            // Set ElementId
            data.Id = elementId;

            // Set OwnerViewId
            data.OwnerViewId = element.OwnerViewId;

            // Set fields related to category
            Category category = element.Category;
            if (category != null)
            {
                data.CategoryType = category.CategoryType.ToString();
                data.Category = category.ToString();
            }

            // Set fields related to elementType
            ElementId typeId = element.GetTypeId();
            if (typeId != null)
            {
                ElementType elementType = doc.GetElement(typeId) as ElementType;
                if (elementType != null)
                {
                    data.Family = elementType.FamilyName;
                    data.ElementType = elementType.Name;
                }
            }

            return data;
        }

        #endregion Auxiliary Functions

    }

}
