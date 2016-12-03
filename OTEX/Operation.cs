using System;
using System.Collections.Generic;
using System.Linq;

namespace OTEX
{
    /// <summary>
    /// A single operation.
    /// </summary>
    [Serializable]
    public sealed class Operation
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Node responsible for the change.
        /// </summary>
        public Guid NodeID
        {
            get { return node; }
        }
        private Guid node;

        /// <summary>
        /// Beginning index of the operation.
        /// </summary>
        public int Offset
        {
            get { return offset; }
        }
        private int offset;

        /// <summary>
        /// String used for insertions. Null for deletions.
        /// </summary>
        public string Text
        {
            get { return text; }
        }
        private string text;

        /// <summary>
        /// Length of character span affected by this operation.
        /// </summary>
        public int Length
        {
            get { return length; }
        }
        private int length;

        /// <summary>
        /// Is this operation a no-op?
        /// </summary>
        public bool IsNoop
        {
            get { return length == 0; }
        }

        /// <summary>
        /// Is this operation an insertion?
        /// </summary>
        public bool IsInsertion
        {
            get { return !IsNoop && text != null; }
        }

        /// <summary>
        /// Is this operation a deletion?
        /// </summary>
        public bool IsDeletion
        {
            get { return !IsNoop && text == null; }
        }

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTION/INITIALIZATION/DESTRUCTION
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Create a text insertion operation.
        /// </summary>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        internal Operation(Guid nodeID, int offset, string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            if (nodeID == Guid.Empty)
                throw new ArgumentOutOfRangeException("nodeID", "nodeID cannot be Guid.Empty");
            node = nodeID;
            this.offset = offset;
            length = text.Length;
            this.text = text;
        }

        /// <summary>
        /// Create a text deletion operation.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException" />
        internal Operation(Guid nodeID, int offset, int length)
        {
            if (nodeID == Guid.Empty)
                throw new ArgumentOutOfRangeException("nodeID", "nodeID cannot be Guid.Empty");
            node = nodeID;
            this.offset = offset;
            this.length = length;
            text = null;
        }

        /// <summary>
        /// Copy an existing operation.
        /// </summary>
        /// <exception cref="ArgumentNullException" />
        internal Operation(Operation operation)
        {
            if (operation == null)
                throw new ArgumentNullException("operation");
            node = operation.node;
            offset = operation.offset;
            length = operation.length;
            text = operation.text;
        }

        /////////////////////////////////////////////////////////////////////
        // TRANSFORMATIONS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Perform a symmetric linear transform (SLOT) on two sets of operations.
        /// </summary>
        /// <exception cref="ArgumentNullException" />
        internal static void SymmetricLinearTransform(IEnumerable<Operation> list1, IEnumerable<Operation> list2)
        {
            if (list1 == null)
                throw new ArgumentNullException("list1");
            if (list2 == null)
                throw new ArgumentNullException("list2");

            if (list1 == null || list1.Count() == 0 || list2 == null || list2.Count() == 0)
                return;

            for (int i = 0; i < list2.Count(); i++)
            {
                for (int j = 0; j < list1.Count(); j++)
                {
                    Operation list2i = new Operation(list2.ElementAt(i));
                    list2.ElementAt(i).TransformAgainst(list1.ElementAt(j));
                    list1.ElementAt(j).TransformAgainst(list2i);
                }
            }
        }

        internal void TransformAgainst(Operation operation)
        {
            if (operation == null)
                throw new ArgumentNullException("operation");

            if (IsInsertion && operation.IsInsertion)
                IT_II(operation);
            else if (IsInsertion && operation.IsDeletion)
                IT_ID(operation);
            else if (IsDeletion && operation.IsInsertion)
                IT_DI(operation);
            else if (IsDeletion && operation.IsDeletion)
                IT_DD(operation);
        }

        /////////////////////////////////////////////////////////////////////
        // TRANSFORMATIONS (pimpl)
        /////////////////////////////////////////////////////////////////////

        private void IT_II(Operation operation)
        {
            if ((offset > operation.offset) || (offset == operation.offset && node.CompareTo(operation.node) > 0))
                offset += operation.length;
        }

        private void IT_ID(Operation operation)
        {
            if (operation.length == 0 || offset <= operation.offset)
                return;

            if (offset > (operation.offset + operation.length))
                offset = offset - operation.length;
            else if (offset == (operation.offset + operation.length))
                offset = operation.offset;
            else
                MakeNoop();
        }

        private void IT_DI(Operation operation)
        {
            if (operation.offset >= offset + length)
                return;

            if (offset >= operation.offset)
                offset += operation.length;
            else
                length += operation.length;
        }

        private void IT_DD(Operation operation)
        {
            if (operation.length == 0 || operation.offset >= offset + length)
                return;

            if (offset >= (operation.offset + operation.length))
                offset -= operation.length;
            else if ((operation.offset <= offset) && (offset + length <= operation.offset + operation.length))
                MakeNoop();
            else if (operation.offset <= offset && offset + length > operation.offset + operation.length)
            {
                var tmpOffset = offset;
                offset = operation.offset;
                length = (tmpOffset + length) - (operation.offset + operation.length);
            }
            else if (operation.offset > offset && operation.offset + operation.length >= offset + length)
                length = operation.offset - offset;
            else if (operation.offset > offset && operation.offset + operation.length < offset + length)
                length = length - operation.length;
        }

        private void MakeNoop()
        {
            length = 0;
            text = null;
        }

        /////////////////////////////////////////////////////////////////////
        // EXECUTION
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Execute an operation on some text, returning the text with the operation applied.
        /// </summary>
        /// <param name="document">The "document" text to execute the operation on.</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <returns>The string resulting from applying the operation to the input string.</returns>
        public string Execute(string document)
        {
            if (IsNoop)
                return document;
            if (document == null)
                throw new ArgumentNullException("document");
            if (offset > document.Length)
                throw new ArgumentOutOfRangeException("offset", "offset cannot be larger than document.length");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", "offset cannot be negative");

            return IsInsertion ? document.Insert(offset, text)
                : (offset == 0 && length >= document.Length ? "" : document.Remove(offset, length));
        }

        /////////////////////////////////////////////////////////////////////
        // MERGING
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Merge this operation with another one, if possible. Assumes the other operation
        /// is the very next element in a sequence of operations.
        /// </summary>
        /// <param name="op">The other operation.</param>
        /// <returns>True if a merge was sucessful.</returns>
        /// <exception cref="ArgumentNullException" />
        internal bool Merge(Operation op)
        {
            if (op == null)
                throw new ArgumentNullException("op");
            if (op.IsNoop)
                return true;

            //both operations are insertions, directly back-to-back
            if (IsInsertion && op.IsInsertion && (offset + length) == op.offset)
            {
                text = text.Substring(0, length) + op.text.Substring(0, op.length);
                length += op.length;
                op.MakeNoop();
                return true;
            }

            //both operations are deletions, directly back-to-back
            if (IsDeletion && op.IsDeletion && offset == op.offset)
            {
                length += op.length;
                op.MakeNoop();
                return true;
            }

            return false;
        }
    }
}