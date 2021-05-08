using System.Collections.Generic;
using System.Text;

namespace GanjooRazor.Utils
{
    /// <summary>
    /// Google BreadCrumbList
    /// </summary>
    public class GoogleBreadCrumbList
    {
        class ItemListElement
        {
            public int Position { get; set; }

            public string Name { get; set; }

            public string Id { get; set; }

            public string Image { get; set; }
        }

        private List<ItemListElement> _items = new List<ItemListElement>();

        /// <summary>
        /// add item
        /// </summary>
        /// <param name="name"></param>
        /// <param name="slug"></param>
        /// <param name="image"></param>
        public void AddItem(string name, string slug, string image)
        {
            _items.Add(new ItemListElement()
            {
                Position = _items.Count + 1,
                Name = name,
                Id = slug,
                Image = image
            });
        }

        /// <summary>
        /// to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(" \"@context\": \"http://schema.org\",");
            sb.AppendLine(" \"@type\": \"BreadcrumbList\",");
            sb.AppendLine(" \"itemListElement\": [");

            for(int i=0; i<_items.Count; i++)
            {
               
                sb.AppendLine("     {");
                sb.AppendLine("     \"@type\": \"ListItem\",");
                sb.AppendLine($"     \"position\": {_items[i].Position},");
                sb.AppendLine("     \"item\": {");
                sb.AppendLine($"         \"@id\": \"https://ganjoor.net{_items[i].Id}/\",");
                sb.AppendLine($"         \"name\": \"{_items[i].Name}\", ");
                sb.AppendLine($"         \"image\": \"{_items[i].Image}\"");

                sb.AppendLine("     }");

                if (i != (_items.Count - 1))
                {
                    sb.AppendLine("    },");
                }
                else
                {
                    sb.AppendLine("    }");
                }
                
            }

            sb.AppendLine("]");

            return sb.ToString();
        }
    }
}
