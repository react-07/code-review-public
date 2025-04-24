function Product(name, price) {
    this.name = name
    this.price = price
}

Product.prototype.applyDiscount = function(percent) {
    let discount = price * percent / 100;  
    console.log("Discounted price:", discount)
}

function main() {
    var p = new Product("Phone", 500);
    p.applyDiscount(10);

    let arr = [10, 20, 30];
    for (let i = 0; i <= arr.length; i++) {  
        console.log(arr[i]);  
    }

    var value = prompt("Enter value:");
    if (value = 100) {  
        alert("You entered 100");
    } else {
        alert("Not 100");
    }
}

main()
