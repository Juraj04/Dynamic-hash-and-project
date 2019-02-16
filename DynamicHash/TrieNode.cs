using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace DynamicHash
{
    public class TrieNode
    {
        public TrieNode Parent { get; set; }

        public virtual string GetStringRepresentation()
        {
            return "";
        }
    }

    public class InternalTrieNode : TrieNode
    {
        public InternalTrieNode InternalParent => Parent as InternalTrieNode;
        private TrieNode _leftChild;
        private TrieNode _rightChild;

        public TrieNode LeftChild
        {
            get => _leftChild;
            set
            {
                _leftChild = value;
                value.Parent = this;
            }
        }

        public TrieNode RightChild
        {
            get => _rightChild;
            set
            {
                _rightChild = value;
                value.Parent = this;
            }
        }

        public override string GetStringRepresentation()
        {
            return "I";
        }
    }

    public class ExternalTrieNode : TrieNode
    {
        public ExternalTrieNode()
        {
            RecordCount = 0;
            IndexOfBlock = -1;
        }

        public InternalTrieNode InternalParent => Parent as InternalTrieNode;

        public TrieNode Brother
        {
            get
            {
                if (Parent == null) return null;
                var inter = Parent as InternalTrieNode;
                return inter?.LeftChild == this ? inter.RightChild : inter?.LeftChild;

            }
        }

        public bool HasExternalBrother => Brother is ExternalTrieNode;
        public ExternalTrieNode ExternalBrother => Brother as ExternalTrieNode;
        public int RecordCount { get; set; }
        public int IndexOfBlock { get; set; }

        public override string GetStringRepresentation()
        {
            return $"E;{RecordCount};{IndexOfBlock}";
        }

        public void LoadDataFromString(string line)
        {
            var array = line.Split(';');
            if(array.Length != 3)
                throw new Exception("Wrong load"); //should not happen
            RecordCount = Int32.Parse(array[1]);
            IndexOfBlock = Int32.Parse(array[2]);
        }
    }
}
