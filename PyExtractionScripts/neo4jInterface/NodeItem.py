class NodeItem:
    """
    reflects the node structure from neo4j
    """

    def __init__(self, node_id: int):
        """
        @param node_id: node id in the graph database
        @param rel_type: rel_type of edge pointing to this node
        """
        self.id = node_id
        self.attrs = None
        self.labels = []
        self.node_identifier = ""

    def get_node_identifier(self):
        if self.node_identifier == "":
            return "n{}".format(self.id)
        else:
            return self.node_identifier

    def __repr__(self):
        return 'NodeItem: id: {} var: {} attrs: {} labels: {}' \
            .format(self.id, self.node_identifier, self.attrs, self.labels)

    def __eq__(self, other):
        """
        implements a comparison function. matching by node id
        @param other:
        @return:
        """
        if self.id == other.id:
            return True
        else:
            return False
        # ToDo: eq comparison is not valid for cypher-based DPO

    @classmethod
    def from_neo4j_response(cls, raw) -> list:
        """
        creates a List of NodeItem instances from a given neo4j response
        @param raw: neo4j response string
        @return:
        """
        ret_val = []
        for node_raw in raw:
            # parse
            node_id = int(node_raw[0])
            node_attributes = node_raw[1]
            node_label = node_raw[2]

            # init instances
            node = cls(node_id=node_id)

            # save label and attributes
            node.attrs = node_attributes
            node.labels.append(node_label)

            ret_val.append(node)

        return ret_val

    def to_cypher(self, skip_attributes=False, skip_labels=False):
        """
        returns a cypher query fragment to search for or to create this node with semantics
        @return:
        """
        cy_node_identifier = self.get_node_identifier()
        cy_node_attrs = ""
        cy_node_labels = ""

        if skip_attributes is False:
            if self.attrs != {}:
                cy_node_attrs = self.format_dict(self.attrs)

        if skip_labels is False:
            if len(self.labels) > 0:
                for label in self.labels:
                    cy_node_labels += ":{}".format(label)

        return '({0}{1}{2})'.format(cy_node_identifier, cy_node_labels, cy_node_attrs)

    def set_node_attributes(self, attrs):
        """
        assigns attributes to node item
        @param attrs: dict or list
        @return: nothing
        """
        if isinstance(attrs, list):
            d = attrs[0]
            self.attrs = d
        else:
            self.attrs = attrs

    @classmethod
    def format_dict(cls, dictionary):
        """
        formats a given dictionary to be understood in a cypher query
        @param dictionary: dict to be formatted
        @return: string representation of dict
        """
        s = "{"

        for key in dictionary:
            s += "{0}:".format(key)
            if isinstance(dictionary[key], dict):
                # Apply formatting recursively
                s += "{0}, ".format(dictionary(dictionary[key]))
            elif isinstance(dictionary[key], list):
                s += "["
                for l in dictionary[key]:
                    if isinstance(l, dict):
                        s += "{0}, ".format(dictionary(l))
                    else:
                        # print(l)
                        if isinstance(l, int):
                            s += "{0}, ".format(l)
                        else:
                            s += "'{0}', ".format(l)
                if len(s) > 1:
                    s = s[0: -2]
                s += "], "
            else:
                if isinstance(dictionary[key], (int, float)):
                    s += "{0}, ".format(dictionary[key])
                else:
                    s += "\"{0}\", ".format(dictionary[key])
        # Quote all the values
        # s += "\'{0}\', ".format(self[key])

        if len(s) > 1:
            s = s[0: -2]
        s += "}"
        return s