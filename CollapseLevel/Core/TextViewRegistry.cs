using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace CollapseLevel.Core
{
    /// <summary>
    /// Keeps references to all instances of IWpfTextView.
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class TextViewRegistry : IWpfTextViewCreationListener
    {
        private static List<IWpfTextView> textViews = new List<IWpfTextView>();

        public static IEnumerable<IWpfTextView> GetExistingViews()
        {
            return textViews.Where(p => !p.IsClosed); // Return only open views.
        }

        public void TextViewCreated(IWpfTextView textView)
        {
            textViews.RemoveAll((it) => it.IsClosed); // Remove references to removed views.
            textViews.Add(textView);
        }
    }
}