using System;
using System.Drawing;

namespace OTEX
{
    /// <summary>
    /// Interface to allow text controls of different types to be used as an OTEX Editor text box.
    /// </summary>
    public interface IEditorTextBox
    {
        /// <summary>
        /// Are the OnInsertion/OnDeletion events fired when the text is changed?
        /// </summary>
        bool DiffEvents { get; set; }

        /// <summary>
        /// Fired when text is inserted into the document (if DiffEvents == true).
        /// parameters: sender, starting index, inserted text.
        /// </summary>
        event Action<IEditorTextBox, int, string> OnInsertion;

        /// <summary>
        /// Fired when text is deleted from the document (if DiffEvents == true).
        /// parameters: sender, starting index, length of deleted text.
        /// </summary>
        event Action<IEditorTextBox, int, int> OnDeletion;

        /// <summary>
        /// The current language definitions used for syntax highlighting.
        /// </summary>
        LanguageManager.Language Language { get; set; }

        /// <summary>
        /// The user colour as applied to this editor text box.
        /// </summary>
        Color UserColour { get; set; }

        /// <summary>
        /// Insert text without altering the current user's caret position or selection range.
        /// </summary>
        /// <param name="offset">Insert postion</param>
        /// <param name="text">New text</param>
        void InsertText(int offset, string text);

        /// <summary>
        /// Delete text without altering the current user's caret position or selection range.
        /// </summary>
        /// <param name="offset">Deletion start position</param>
        /// <param name="length">Deletion length</param>
        void DeleteText(int offset, int length);

        /// <summary>
        /// Clear the undo history stack.
        /// </summary>
        void ClearUndo();

        /// <summary>
        /// Contents of this text box.
        /// </summary>
        string Text { get; set; }
    }
}