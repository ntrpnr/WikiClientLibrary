﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;

namespace WikiClientLibrary.Generators
{
    /// <summary>
    /// List of pages that belong to a given category, ordered by page sort title.
    /// </summary>
    public class CategoryMembersGenerator : PageGenerator<WikiPage>
    {
        // We cannot decide whether generated item is a page or category,
        // so we just use the base class Page for PageGenerator<T>.

        public CategoryMembersGenerator(WikiSite site) : base(site)
        {
        }

        /// <summary>
        /// Initializes the generator from the site and category title.
        /// </summary>
        /// <param name="site">Site instance.</param>
        /// <param name="categoryTitle">Title of the category, with or without Category: prefix.</param>
        public CategoryMembersGenerator(WikiSite site, string categoryTitle) : base(site)
        {
            CategoryTitle = WikiLink.NormalizeWikiLink(site, categoryTitle, BuiltInNamespaces.Category);
        }

        /// <summary>
        /// Initializes <see cref="CategoryTitle"/> from specified <see cref="CategoryPage"/> .
        /// </summary>
        public CategoryMembersGenerator(CategoryPage category) : base(category.Site)
        {
            if (category == null) throw new ArgumentNullException(nameof(category));
            CategoryTitle = category.Title;
        }

        /// <summary>
        /// The category to enumerate (required unless cmpageid is used).
        /// Must include "Category:" prefix. Cannot be used together with cmpageid.
        /// </summary>
        public string CategoryTitle { get; set; }

        /// <summary>
        /// Only list pages in these namespaces.
        /// </summary>
        /// <value>Selected ids of namespace, or null if all the namespaces are selected.</value>
        public IEnumerable<int> NamespaceIds { get; set; }

        /// <summary>
        /// Type of category members to include. Ignored when cmsort=timestamp is set.
        /// </summary>
        /// <value>Defaults to <see cref="CategoryMemberTypes.All"/> .</value>
        public CategoryMemberTypes MemberTypes { get; set; } = CategoryMemberTypes.All;

        private string ParseMemberTypes(CategoryMemberTypes value)
        {
            var types = "";
            if ((value & CategoryMemberTypes.Page) == CategoryMemberTypes.Page) types += "|page";
            if ((value & CategoryMemberTypes.Subcategory) == CategoryMemberTypes.Subcategory) types += "|subcat";
            if ((value & CategoryMemberTypes.File) == CategoryMemberTypes.File) types += "|file";
            if (types.Length == 0) throw new ArgumentOutOfRangeException(nameof(value));
            return types.Substring(1);
        }

        /// <summary>
        /// When overridden, fills generator parameters for action=query request.
        /// </summary>
        /// <param name="actualPagingSize"></param>
        /// <returns>The dictioanry containing request value pairs.</returns>
        protected override IEnumerable<KeyValuePair<string, object>> GetGeneratorParams(int actualPagingSize)
        {
            if (string.IsNullOrEmpty(CategoryTitle)) throw new InvalidOperationException("CateogryTitle is empty.");
            return new Dictionary<string, object>
            {
                {"generator", "categorymembers"},
                {"gcmtitle", CategoryTitle},
                {"gcmlimit", GetActualPagingSize()},
                {"gcmnamespace", NamespaceIds == null ? null : string.Join("|", NamespaceIds)},
                {"gcmtype", ParseMemberTypes(MemberTypes)}
            };
        }
    }

    /// <summary>
    /// Types of category members.
    /// </summary>
    [Flags]
    public enum CategoryMemberTypes
    {
        /// <summary>
        /// Invalid member type. Attempt to use this value will cause exceptions.
        /// </summary>
        Invalid = 0,
        Page = 1,
        Subcategory = 2,
        File = 4,
        All = Page | Subcategory | File,
    }
}
