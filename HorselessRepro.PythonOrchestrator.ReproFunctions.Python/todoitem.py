class ToDoItem:
    def __init__(
        self,
        id: str,
        Description: str,
        PartitionKey: str = "ToDoItem",
        _ts: int = None,
        _lsn: int = None,
        _rid: str = None,
        _etag: str = None
    ):
        self.id = id
        self.Description = Description
        self.PartitionKey = PartitionKey
        self._ts = _ts
        self._lsn = _lsn
        self._rid = _rid
        self._etag = _etag

    def to_dict(self):
        return {
            "id": self.id,
            "Description": self.Description,
            "PartitionKey": self.PartitionKey,
            "_ts": self._ts,
            "_lsn": self._lsn,
            "_rid": self._rid,
            "_etag": self._etag
        }

    @staticmethod
    def from_dict(data):
        return ToDoItem(
            id=data.get("id"),
            Description=data.get("Description"),
            PartitionKey=data.get("PartitionKey", "ToDoItem"),
            _ts=data.get("_ts"),
            _lsn=data.get("_lsn"),
            _rid=data.get("_rid"),
            _etag=data.get("_etag")
        )