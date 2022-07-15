from pprint import pprint

import neo4jInterface.neo4jConnector
from Extractors.AccessibityExtractor import AccessibilityExtractor
from neo4jInterface.NodeItem import NodeItem


def write_neo4j_to_csv():

    # connect to DB
    connector = neo4jInterface.neo4jConnector.Neo4jConnector()
    connector.connect_driver()

    # get all nodes
    cy = "MATCH (n) WHERE LABELS(n)[0] IN  [\"Room\", \"Door\", \"Stair\", \"Ramp\"] " \
         "RETURN ID(n), PROPERTIES(n), LABELS(n)[0] ORDER BY LABELS(n)[0]"
    res = connector.run_cypher_statement(cy)
    nodes = NodeItem.from_neo4j_response(res)

    # get relevant edges/paths
    cy = "MATCH pattern1 =  (r1:Room)<--(w:Wall)<--(d:Door), " \
         "pattern2 = (r2:Room)<--(w) WHERE r1.ElementId < r2.ElementId RETURN [ID(d), ID(r1)], [ID(d), ID(r2)]"
    edges = connector.run_cypher_statement(cy)
    pprint(edges)

    AccessibilityExtractor.write_to_csv(nodes, edges)


if __name__ == '__main__':
    write_neo4j_to_csv()


