using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TempustTerrain
{
    public class DoubleLinkedList<T>
    {
        List<DoubleLinkedListNode> list;
        public DoubleLinkedListNode Last { get; set; }
        public DoubleLinkedListNode First { get; set; }

        public int Count { get; private set; }

        public DoubleLinkedList()
        {
            list = new List<DoubleLinkedListNode>();
        }

        public void AddLast(T add)
        {
            DoubleLinkedListNode node = new DoubleLinkedListNode(add);
            if (First == null)
            {
                First = node;
                Last = node;
            }

            First.Previous = node;
            Last.Next = node;
            node.Previous = Last;
            node.Next = First;
            Last = node;
            Count++;
        }

        public void RemoveAt(int index)
        {
            DoubleLinkedListNode node = list[index];
            Remove(node);
        }

        public void Remove(DoubleLinkedListNode node)
        {
            node.Previous.Next = node.Next;
            node.Next.Previous = node.Previous;

            if (node == First)
                First = node.Next;
            if (node == Last)
                Last = node.Previous;

            list.Remove(node);
            Count--;
        }

        public class DoubleLinkedListNode
        {
            public T Value { get; set; }
            public DoubleLinkedListNode Next { get; set; }
            public DoubleLinkedListNode Previous { get; set; }

            public DoubleLinkedListNode(T value)
            {
                Value = value;
            }
        }
    }
}
