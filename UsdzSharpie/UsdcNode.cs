using System;
using System.Collections.Generic;
using System.Text;

namespace UsdzSharpie
{
    public class UsdcNode
    {
        public enum NodeType
        {
            NODE_TYPE_NULL = 0,
            NODE_TYPE_XFORM,
            NODE_TYPE_GROUP,
            NODE_TYPE_GEOM_MESH,
            NODE_TYPE_MATERIAL,
            NODE_TYPE_SHADER,
            NODE_TYPE_CUSTOM,   // Uer defined custom node

        };

        private long _parent;  // -1 = this node is the root node. -2 = invalid or leaf node
        private List<long> _children = new List<long>();  // index to child nodes.
        private List<string>  _primChildren = new List<string>(); // List of name of child nodes

        private UsdcPath _path;  // local path

        private NodeType _node_type;

        public long GetParent()
        {
            return _parent;
        }

        public long[] GetChildren()
        {
            return _children.ToArray();
        }

        public void AddChildren(string childName, long nodeIndex)
        {
            //assert(_primChildren.count(child_name) == 0);
            _primChildren.Add(childName);
            _children.Add(nodeIndex);
        }

        public string GetLocalPath()
        {
            return _path.full_path_name();
        }

        public UsdcPath GetPath()
        {
            return _path;
        }

        public NodeType GetNodeType()
        {
            return _node_type;
        }

        public string[] GetPrimChildren()
        {
            return _primChildren.ToArray();
        }

        public UsdcNode()
        {
            _parent = -2;
        }

        public UsdcNode(long parent, UsdcPath path)
        {
            _parent = parent;
            _path = path;
        }
    }
}
