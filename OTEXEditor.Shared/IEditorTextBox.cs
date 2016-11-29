using System;
using System.Drawing;

namespace OTEX.Editor
{
    /// <summary>
    /// Interface to allow text controls of different types to be used as an OTEX Editor text box.
    /// Text editor plugins must expose a class that implements this interface a well as a default constructor.
    /// </summary>
    public interface IEditorTextBox
    {
        /////////////////////////////////////////////////////////////////////
        // EVENTS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Fired when text is inserted into the document (if DiffEvents == true).
        /// parameters: sender, starting index, inserted text.
        /// </summary>
        event Action<IEditorTextBox, uint, string> OnInsertion;

        /// <summary>
        /// Fired when text is deleted from the document (if DiffEvents == true).
        /// parameters: sender, starting index, length of deleted text.
        /// </summary>
        event Action<IEditorTextBox, uint, uint> OnDeletion;

        /// <summary>
        /// Fired when the user's current selection changes.
        /// parameters: sender, starting index, ending index.
        /// </summary>
        event Action<IEditorTextBox, uint, uint> OnSelection;

        /////////////////////////////////////////////////////////////////////
        // PROPERTIES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Are the OnInsertion/OnDeletion events fired when the text is changed?
        /// </summary>
        bool DiffEvents { get; set; }

        /// <summary>
        /// The current language definitions used for syntax highlighting.
        /// </summary>
        LanguageManager.Language Language { get; set; }

        /// <summary>
        /// The user colour as applied to this editor text box.
        /// </summary>
        Color UserColour { get; set; }

        /// <summary>
        /// Are line ending characters visible?
        /// </summary>
        bool LineEndingsVisible { get; set; }

        /////////////////////////////////////////////////////////////////////
        // METHODS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Insert text without altering the current user's caret position or selection range.
        /// </summary>
        /// <param name="offset">Insert postion</param>
        /// <param name="text">New text</param>
        void InsertText(uint offset, string text);

        /// <summary>
        /// Delete text without altering the current user's caret position or selection range.
        /// </summary>
        /// <param name="offset">Deletion start position</param>
        /// <param name="length">Deletion length</param>
        void DeleteText(uint offset, uint length);

        /// <summary>
        /// Clear the contents of the text box.
        /// </summary>
        void ClearText();

        /// <summary>
        /// Clear the undo history stack.
        /// </summary>
        void ClearUndoHistory();

        /// <summary>
        /// Change settings for the user's line-length ruler.
        /// </summary>
        /// <param name="visible">Draw the ruler?</param>
        /// <param name="offset">Line-length offset (column) of the ruler.</param>
        void SetRuler(bool visible, uint offset);

        /// <summary>
        /// Set/update a custom highlight range for a specific id.
        /// </summary>
        /// <param name="id">Unique id of range.</param>
        /// <param name="start">Range start index</param>
        /// <param name="end">Range end index</param>
        /// <param name="colour">Colour to use for highlighting</param>
        void SetHighlightRange(Guid id, uint start, uint end, Color colour);

        /// <summary>
        /// Clears all custom highlight ranges.
        /// </summary>
        void ClearHighlightRanges();

        /// <summary>
        /// Append a "line comment" prefix to the beginning of every line in the current selection,
        /// according to the currently selected Language.
        /// </summary>
        void CommentSelection();

        /// <summary>
        /// Detect the "line comment" status of the currently selected block of text and invert it,
        /// according to the currently selected Language.
        /// </summary>
        void ToggleCommentSelection();

        /// <summary>
        /// Remove "line comment" prefixes from the beginning of every line in the current selection,
        /// according to the currently selected Language.
        /// </summary>
        void UncommentSelection();

        /// <summary>
        /// Toggle the "bookmarked" status of the caret's line.
        /// </summary>
        void ToggleBookmark();
        
        /// <summary>
        /// Move the caret to the next bookmark.
        /// </summary>
        void NextBookmark();

        /// <summary>
        /// Move the caret to the previous bookmark.
        /// </summary>
        void PreviousBookmark();

        /// <summary>
        /// Increase the current zoom level.
        /// </summary>
        void IncreaseZoom();

        /// <summary>
        /// Decrease the current zoom level.
        /// </summary>
        void DecreaseZoom();

        /// <summary>
        /// Reset the zoom level back to zero.
        /// </summary>
        void ResetZoom();

        /// <summary>
        /// Convert all currently selected characters to uppercase.
        /// </summary>
        void UppercaseSelection();

        /// <summary>
        /// Convert all currently selected characters to lowercase.
        /// </summary>
        void LowercaseSelection();
    }
}