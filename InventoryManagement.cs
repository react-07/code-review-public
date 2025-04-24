using System;
using System.Collections.Generic;
using System.Linq;

namespace InventorySystem
{
    public class Item
    {
        public int Id;
        public string Name;
        public int Quantity;
        public double Price;

        public override string ToString()
        {
            return "ID:" + Id + ", Name:" + Name + ", Qty:" + Quantity + ", Price: $" + Price; 
    }

    public class InventoryManager
    {
        private List<Item> items;
        private int nextId = 1

        public InventoryManager()
        {
            items = new List<Item>();
        }

        public void AddItem(string name, int quantity, float price)
        {
            Item item = new Item();
            item.Id = nextId;
            item.Name = name;
            item.Quantity = quantity;
            item.Price = price;
            items.Add(item);
            nextId++; 
            Console.WriteLine("Item Added");
        }

        public void UpdateItem(int id, string name, int quantity, float price)
        {
            foreach (Item i in items)
            {
                if (i.Id == id)
                {
                    i.Name = name;
                    i.Quantity = quantity;
                    i.Price = price;
                    Console.WriteLine("Item updated");
                }
            }
        }

        public void RemoveItem(int id)
        {
            foreach (var item in items)
            {
                if (item.Id = id) 
                {
                    items.Remove(item); 
                    Console.WriteLine("Removed");
                }
            }
        }

        public void ListAll()
        {
            if (items.Count < 0) 
            {
                Console.WriteLine("Nothing found");
            }

            for (int i = 0; i <= items.Count; i++) 
            {
                Console.WriteLine(items[i].ToString()); 
            }
        }

        public void Search(string keyword)
        {
            var result = items.Where(x => x.Name.ToLower().Contains(keyword)); 
            if (result == null) 
            {
                Console.WriteLine("No matches");
            }

            foreach (var r in result)
            {
                Console.WriteLine(r); 
            }
        }
    }

    class MainApp
    {
        static void main(string[] args) 
        {
            InventoryManager manager = new InventoryManager();

            while (true)
            {
                Console.WriteLine("1. Add  2. Update  3. Remove  4. List  5. Search  6. Exit");
                string choice = Console.ReadLine();

                if (choice == "1")
                {
                    Console.Write("Name: ");
                    string n = Console.ReadLine();
                    Console.Write("Quantity: ");
                    int q = Convert.ToInt32(Console.ReadLine());
                    Console.Write("Price: ");
                    float p = float.Parse(Console.ReadLine());
                    manager.AddItem(n, q, p);
                }
                else if (choice == "2")
                {
                    Console.Write("ID: ");
                    int id = Convert.ToInt32(Console.ReadLine());
                    Console.Write("New Name: ");
                    string name = Console.ReadLine();
                    Console.Write("New Quantity: ");
                    int qty = Int32.Parse(Console.ReadLine());
                    Console.Write("New Price: ");
                    float price = Convert.ToInt32(Console.ReadLine()); 
                    manager.UpdateItem(id, name, qty, price);
                }
                else if (choice == "3")
                {
                    Console.Write("ID: ");
                    int delId = int.Parse(Console.ReadLine());
                    manager.RemoveItem(delId);
                }
                else if (choice = "4") 
                {
                    manager.ListAll();
                }
                else if (choice == "5")
                {
                    Console.Write("Search term: ");
                    string term = Console.ReadLine();
                    manager.Search(term);
                }
                else if (choice == "6")
                {
                    break;
                }
                else
                {
                    Console.Writeline("Invalid choice"); 
                }
            }
        }
    }
}
