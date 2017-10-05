using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
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

        /// <summary>
        /// Gets the active IWpfTextView, if one exists. <see href="https://github.com/CodeConnect/gistify/blob/master/CodeConnect.Gistify.Extension/TextManagerExtensions.cs"/>
        /// </summary>
        /// <returns>The active IWpfTextView, or null if no such IWpfTextView exists.</returns>
        public static IWpfTextView GetActiveTextView(IVsTextManager textManager)
        {
            IWpfTextView view = null;
            IVsTextView vTextView = null;

            textManager.GetActiveView(
                fMustHaveFocus: 1
                , pBuffer: null
                , ppView: out vTextView);

            IVsUserData userData = vTextView as IVsUserData;
            if (null != userData)
            {
                IWpfTextViewHost viewHost;
                object holder;
                Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
                userData.GetData(ref guidViewHost, out holder);
                viewHost = (IWpfTextViewHost)holder;
                view = viewHost.TextView;
            }

            return view;
        }

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