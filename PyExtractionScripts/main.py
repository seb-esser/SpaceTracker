from pprint import pprint

import neo4jInterface.neo4jConnector
from neo4jInterface.NodeItem import NodeItem


def write_neo4j_to_csv():

    # connect to DB
    connector = neo4jInterface.neo4jConnector.Neo4jConnector()
    connector.connect_driver()

    # get all nodes
    cy = "match (n) return PROPERTIES(n), LABELS(n)[0] ORDER BY LABELS(n)[0]"
    res = connector.run_cypher_statement(cy)
    nodes = NodeItem.from_neo4j_response(res)
    pprint(nodes)


# Press the green button in the gutter to run the script.
if __name__ == '__main__':
    write_neo4j_to_csv()


