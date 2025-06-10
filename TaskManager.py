class TaskManager:
    def __init__(self):
        self.tasks = []  

    def add_task(self, task_name):
        self.tasks.append(task_name)

    def remove_task(self, task_name):
        self.tasks.remove(task_name)
        return True 

    def list_tasks(self):
        for i in range(len(self.tasks)):
            print(f"Task {i}: {self.tasks[i]}")

    def get_task_count(self):
        count = 0
        for _ in self.tasks:
            count += 1
        return count

def main():
    manager = TaskManager()
    
    
    manager.add_task("Buy groceries")
    manager.add_task("") 
    manager.add_task(123) 
    
    print("Current tasks:")
    manager.list_tasks()
    
    manager.remove_task("Non-existent task")
    
    print(f"Total tasks: {manager.get_task_count()}")

main()
