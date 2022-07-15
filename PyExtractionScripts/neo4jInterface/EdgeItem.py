from typing import Dict
import re

from neo4jInterface.NodeItem import NodeItem
from neo4jInterface.neo4jConnector import Neo4jConnector


class EdgeItem:
    def __init__(self, start_node: NodeItem, end_node: NodeItem, edge_id: int):
        self.start_node: NodeItem = start_node
        self.end_node: NodeItem = end_node
        self.edge_id: int = edge_id
        self.attributes: dict = {}
        self.labels = []
        self.edge_identifier = ""
        self.is_undirected = False

    def __repr__(self):
        return 'EdgeItem: id: {} var: {} fromNode: {} toNode {} attrs: {} labels: {}' \
            .format(
            self.edge_id, self.edge_identifier, self.start_node.id, self.end_node.id, self.attributes, self.labels)

    def __eq__(self, other):
        """
        compares two edges and returns true if both edges are considered as equal
        @param other:
        @return:
        """

        id_equal = self.edge_id == other.edge_id

        return id_equal

    @classmethod
    def from_neo4j_response(cls, raw: str, nodes):
        """
        returns a list of EdgeItem instances from a given neo4j response string
        @raw: the neo4j response
        @nodes: a list of nodeItem instances
        @return: a list of EdgeItem instances in a list
        """

        edges = []

        for edge in raw:
            raw_startnode_id = edge.start_node.id
            raw_endnode_id = edge.end_node.id
            edge_id = edge.id
            attrs = edge._properties
            labels = [edge.type]
            identifier = "e{}".format(edge_id)

            start_node = next(x for x in nodes if x.id == raw_startnode_id)
            end_node = next(x for x in nodes if x.id == raw_endnode_id)

            e = cls(start_node, end_node, edge_id)
            e.attributes = attrs
            e.labels = labels
            e.edge_identifier = identifier
            edges.append(e)

        return edges

    def set_attributes(self, attrs: Dict):
        """
        sets the attribute property
        @param attrs: queried dictionary from neo4j graph
        @return: Nothing
        """
        self.attributes = attrs

    def to_cypher(self,
                  skip_start_node=False,
                  skip_end_node=False,
                  skip_start_node_attrs=False,
                  skip_end_node_attrs=False,
                  skip_start_node_labels=False,
                  skip_end_node_labels=False,
                  skip_edge_attrs=False) -> str:
        """
        returns a cypher statement to search for this edge item.
        @return: cypher statement as str
        """

        # pre-define components of query

        cy_start_node = ""
        cy_edge_identifier = ""
        cy_edge_labels = ""
        cy_edge_attributes = ""
        cy_directed = ""
        cy_end_node = ""

        # parse nodes
        if skip_start_node is False:
            cy_start_node = self.start_node.to_cypher(skip_attributes=skip_start_node_attrs,
                                                      skip_labels=skip_start_node_labels)
        else:
            cy_start_node = ''

        if skip_end_node is False:
            cy_end_node = self.end_node.to_cypher(skip_attributes=skip_end_node_attrs,
                                                  skip_labels=skip_end_node_labels)
        else:
            cy_end_node = ''

        # parse edge attributes, labels and identifiers
        cy_edge_identifier = self.edge_identifier

        if self.attributes != {}:
            if skip_edge_attrs is False:
                cy_edge_attributes = Neo4jConnector.format_dict(self.attributes)

        if len(self.labels) > 0:
            for label in self.labels:
                cy_edge_labels += ":{}".format(label)

        # parse direction
        if self.is_undirected is False:
            cy_directed = ">"

        # construct statement
        cy = '{}-[{}{}{}]-{}{}'.format(
            cy_start_node,
            cy_edge_identifier,
            cy_edge_labels,
            cy_edge_attributes,
            cy_directed,
            cy_end_node
        )

        return cy
