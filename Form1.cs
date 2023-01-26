using ShoppingHelper.Classes;
using System;
using System.Diagnostics;
using System.Globalization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace ShoppingHelper
{
    public partial class Form1 : Form
    {
        ShoppingHelper.Classes.ShoppingHelper shoppingHelper = new();
        List<Classes.Item> list;
        Classes.ItemHelper helper = new();
        ItemComparer comparer;
        public Form1()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("ja-JP");
            InitializeComponent();
            wishListRB.Checked = true;
            list = helper.Products();
            LoadItemsData();
            comparer = new ItemComparer();
            listView1.ListViewItemSorter = comparer;
        }

        public void LoadItemsData()
        {       
            listView1.Columns.Clear();
            listView1.Items.Clear();    
            listView1.ListViewItemSorter = comparer;
            listView1.View = View.Details;
            // Display grid lines.
            listView1.GridLines = true;
            string[] name = list.Select(c => c.Name.ToString()).ToArray();
            string[] vendor = list.Select(c => c.Vendor.ToString()).ToArray();
            string[] price = list.Select(c => c.price.ToString("c")).ToArray();
            string[] link = list.Select(c => c.Name.ToString()).ToArray();
            List<ListViewItem> lvi = new List<ListViewItem>();
            for (int i = 0; i < name.Length; i++)
            {
                ListViewItem item = new ListViewItem(name[i], i);
                item.SubItems.Add(price[i]);
                item.SubItems.Add(vendor[i]);
                item.SubItems.Add(link[i]);
                lvi.Add(item);
            }
            listView1.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView1.Columns.Add("Price", -2, HorizontalAlignment.Left);
            listView1.Columns.Add("Vendor", -2, HorizontalAlignment.Center);

            listView1.Items.AddRange(lvi.ToArray());
            listView1.ColumnClick += SortListView;
            listView1.ItemSelectionChanged += LoadItemIndex;
            listView1.SelectedIndexChanged += LoadProductFromWishlist;
        }

        private void LoadListData()
        {
            listView1.ListViewItemSorter = comparer;
            listView1.View = View.Details;
            // Display grid lines.
            listView1.GridLines = true;
            string dataString = "";
            List<Classes.Item> items = new List<Classes.Item>();    
            if(dataString!= null)
            {
                string[] s = dataString.Split(",");
                foreach (string c in s)
                {   
                    if(c.Length > 1)
                    {
                        Debug.WriteLine($"Searching for {c}");
                        string queryItem = c;
                        Classes.Item product = helper.QueriedItem(c);
                        items.Add(product); 
                    }         
                }
                string[] name = items.Select(c => c.Name.ToString()).ToArray();
                string[] vendor = items.Select(c => c.Vendor.ToString()).ToArray();
                string[] price = items.Select(c => c.price.ToString("c")).ToArray();
                string[] link = items.Select(c => c.Name.ToString()).ToArray();
                List<ListViewItem> lvi = new List<ListViewItem>();
                for (int i = 0; i < name.Length; i++)
                {
                    ListViewItem item = new ListViewItem(name[i], i);
                    item.SubItems.Add(price[i]);
                    item.SubItems.Add(vendor[i]);
                    item.SubItems.Add(link[i]);
                    lvi.Add(item);
                }
                listView1.Columns.Add("Name", -2, HorizontalAlignment.Left);
                listView1.Columns.Add("Price", -2, HorizontalAlignment.Left);
                listView1.Columns.Add("Vendor", -2, HorizontalAlignment.Center);
                
                listView1.Items.AddRange(lvi.ToArray());
                listView1.ColumnClick += SortListView;
                listView1.ItemSelectionChanged += LoadItemIndex;
                listView1.SelectedIndexChanged += LoadProductFromWishlist;
            }
        }

        private void LoadItemIndex(object? sender, ListViewItemSelectionChangedEventArgs e)
        {
            ListView parent = (ListView)sender;
            int index = e.ItemIndex;
            shoppingHelper.currentItem = helper.QueriedItem(parent.Items[index].SubItems[0].Text);
        }

        private void LoadProductFromWishlist(object? sender, EventArgs e)
        {   
            if(shoppingHelper.currentItem != null)
            {
                Classes.Item item = shoppingHelper.currentItem;
                shoppingHelper.DisplayProduct(item);
            }
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
          

        }

        private async void AddItemWishList(object sender, EventArgs e)
        {
            if (urlInputTB.Text != "")
            {
                var task = await shoppingHelper.ProductRetrievedFromURI(urlInputTB.Text);
                if (task)
                {
                    if (shoppingHelper.currentItem != null)
                    {
                        ItemHelper handler = new();
                        Debug.WriteLine("Calling handler.....");
                        var task1 = await handler.AddedToWishList(shoppingHelper.currentItem);
                        if(task1 && wishListRB.Checked)
                        {
                            list.Clear();
                            list = helper.Products();
                            LoadItemsData();
                        }
                    }
                    if (shoppingHelper.currentItem == null)
                    {
                        Debug.WriteLine("Null item");
                    }

                }
            }   
        }

        private void SortListView(object sender, ColumnClickEventArgs e)
        {
            Debug.WriteLine($"{e.Column} clicked");
            int columnIndex = e.Column;
            if (e.Column == comparer.SortColumn)
            {

                if (comparer.SortColumn == columnIndex)
                {

                }
                // Reverse the current sort direction for this column.
                if (comparer.Order == SortOrder.Ascending)
                {
                    comparer.Order = SortOrder.Descending;
                }
                else
                {
                    comparer.Order = SortOrder.Ascending;
                }
            }
            else
            {
                comparer.SortColumn = e.Column;
                comparer.Order = SortOrder.Ascending;
            }
            ListView parent = (ListView)sender;
            parent.Sort();
        }


    }
}