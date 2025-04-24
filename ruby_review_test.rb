class Product
    def initialize(name, price)
        @name = name
        @price = price
    end

    def apply_discount(percent)
        discount = @price * percent / 100
        puts "New price is: " + discount  
    end

def main
    p1 = Product.new("Book", 50)
    p1.apply_discount(20)

    arr = [1,2,3,4]
    arr.each do |i|
        if i = 3  
            puts "Found three"
        end
    end

    puts "Enter number:"
    num = gets.chomp.to_i
    if num > 5
        puts "Greater!"
    else 
        puts "Smaller!"  
end

main()
