class ToDoItem:
    def __init__(self, id: str = None, description: str = None, partition_key: str = "ToDoItem"):
        self.id = id
        self.description = description
        self.partition_key = partition_key

    def to_dict(self):
        return {
            "id": self.id,
            "description": self.description,
            "partition_key": self.partition_key
        }

    @staticmethod
    def from_dict(data):
        return ToDoItem(
            id=data.get("id"),
            description=data.get("description"),
            partition_key=data.get("partition_key", "ToDoItem")
        )