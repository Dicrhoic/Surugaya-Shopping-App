using Microsoft.EntityFrameworkCore.Diagnostics;
using ShoppingHelper.Classes;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace ShoppingHelper
{
    public partial class Form1 : Form
    {
        ShoppingHelper.Classes.ShoppingHelper shoppingHelper = new();
        List<Classes.Item> list;
        Classes.ItemHelper helper = new();
        WishList? wishList;
        ItemComparer comparer;
        private ToolStripProgressBar toolStripProgressBar;
        private ToolStripStatusLabel toolStripStatusLabel;
        int count = -1;
        public Form1()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("ja-JP");
            InitializeComponent();
            list = helper.Products();
            LoadItemsData();
            comparer = new ItemComparer();
            listView1.ListViewItemSorter = comparer;
            InitialiseStatusStrip();
        }

        private void InitialiseStatusStrip()
        {
            toolStripProgressBar = new ToolStripProgressBar();
            toolStripProgressBar.Enabled = false;
            toolStripStatusLabel = new ToolStripStatusLabel();
            toolStripProgressBar.Dock = DockStyle.Fill;
            statusStrip.Items.Add(toolStripProgressBar);
            statusStrip.Items.Add(toolStripStatusLabel);
                     
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
            listView1.Columns.Clear();
            listView1.Items.Clear();
            listView1.ListViewItemSorter = comparer;
            listView1.View = View.Details;
            wishList = helper.WishListData();
            // Display grid lines.
            listView1.GridLines = true;
            string dataString = wishList.productString;
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

        private async void LoadItem(object sender, EventArgs e)
        {
            if(shoppingHelper.currentItem != null)
            {
                urlInputTB.Text = String.Empty;
                var task = await shoppingHelper.ProductRetrievedFromURI(shoppingHelper.currentItem.link);

            }
            if(urlInputTB.Text != String.Empty)
            {
                var task = await shoppingHelper.ProductRetrievedFromURI(urlInputTB.Text);
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

        private async void RemoveItemFromWL(object sender, EventArgs e)
        {
            if (shoppingHelper.currentItem != null)
            {
                ItemHelper handler = new();
                Debug.WriteLine("Calling handler.....");
                var task1 = await handler.RemovedFromWishList(shoppingHelper.currentItem);
                if (task1 && wishListRB.Checked)
                {
                    list.Clear();
                    list = helper.Products();
                    LoadListData();
                }
            }
            if (shoppingHelper.currentItem == null)
            {
                Debug.WriteLine("Null item");
            }
        }


        private async void UpdateItemsData(object sender, EventArgs e)
        {
            sidePanel.Enabled = false;
            addItemBtn.Enabled = false; 
            Debug.WriteLine(list.Count);
            if(list.Count != 0) 
            {
                count = list.Count;
                var task = await PricesUpdated();
                if(task)
                {
                    sidePanel.Enabled =  true;
                    addItemBtn.Enabled = true;
                    toolStripProgressBar.Value = 0;
                    toolStripStatusLabel.Text = "";
                    string msg = $"Updated the prices of {count} items";
                    string cp = $"Update Price Succesful";
                    MBHelper mB = new();
                    mB.SuccessMB(msg, cp);  
                }
            }
        }

        private async Task<bool> PricesUpdated()
        {
            ItemHelper handler = new();
            int delay = 250;                     
            for (int i = 0; i < count; ++i)
            {
                double increment = 100/count;
                if(count > 50)
                {
                    increment = 0.1;
                    delay = 300;
                }
                int step = (int)(increment * i);               
                try
                {
                    toolStripProgressBar.Value = step;
                    toolStripStatusLabel.Text = $"Updating item:{list[i].Name}...{i}/{count}";
                    Classes.Item item = list[i];
                    string link = item.link;
                    var task1 = await shoppingHelper.ProductRetrievedFromURI(link);
                    if(task1 && shoppingHelper.currentItem != null)
                    {
                        var task2 = await handler.UpdatedItem(shoppingHelper.currentItem);
                    }
                    
                    await Task.Delay(delay);
                   
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return false;
                }
              
            }
            toolStripProgressBar.Value = 100;
            toolStripStatusLabel.Text = "Done";
            if (itemsRB.Checked)
            {
                LoadItemsData();
            }
            if (wishListRB.Checked)
            {
                LoadListData();
            }
            return true;
        }
      
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar.Value = e.ProgressPercentage;
            toolStripStatusLabel.Text = e.UserState as String;
        }


        private void ChangeListData(object sender, EventArgs e)
        {   
            
            if(itemsRB.Checked)
            {   
                LoadItemsData();
            }
            if(wishListRB.Checked)
            {
                LoadListData();
            }
            if(cartRB.Checked)
            {
                
            }
        }
    }
}