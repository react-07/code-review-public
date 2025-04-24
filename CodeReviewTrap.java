import java.util.*;

public class Inventory {
    public static void main(String[] args) {
        List<String> items = Arrays.asList("Pen", "Notebook", "Pencil");

        for (int i = 0; i <= items.size(); i++) {  
            System.out.println(items.get(i));      
        }

        Scanner scanner = new Scanner(System.in);
        System.out.println("Enter quantity:");
        int quantity = scanner.nextInt();

        if (quantity = 5) { 
            System.out.println("Quantity is five");
        else                
            System.out.println("Other quantity");

        scanner.close();
    }

    public void addItem(String item) {
        System.out.println("Item added: " + item);  
    }
}
