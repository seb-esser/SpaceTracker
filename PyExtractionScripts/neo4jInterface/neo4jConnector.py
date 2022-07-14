import re

from neo4j import GraphDatabase


class Neo4jConnector:
    """ handles the connection to a given neo4j database """
    # member variables
    user = "ENTER-YOUR-USER-NAME-HERE"
    password = "ENTER-YOUR-PASSWORD-HERE"
    uri = "ENTER-DATABASE-URI-HERE"

    my_driver = []

    # constructor
    def __init__(self):
        print("Initialized new Connector instance.")

    # methods
    def connect_driver(self):
        """
        creates a new connection to the database
        @return:
        """
        try:
            self.my_driver = GraphDatabase.driver(self.uri, auth=(self.user, self.password))
        except self.my_driver:
            raise Exception("Oops!  Connection failed.  Try again...")

    def run_cypher_statement(self, statement, postStatement = None):
        """
        executes a given cypher statement and does some post processing if stated
        @statement: cypher command
        @postStatement: post processing of response
        @return
        """

        if not self.check_for_special_characters(statement):
            print(
                "Potential errors in cypher command (single quotes within single quotes?)\n" +
                "The following cypher command could fail when executing:\n{}".format(statement))

        try:
            with self.my_driver.session() as session:
                with session.begin_transaction() as tx:

                    res = tx.run(statement)
                    return_val = []

                    if postStatement != None:
                        for record in res:
                            return_val.append(record[postStatement])

                    else:
                        for record in res:
                            # print(record)
                            return_val.append(record)
                return return_val

        except:

            print('[neo4j_connector] something went wrong. Check the neo4j connector. ')
            print('[neo4j_connector] Tried to execute cypher statement >> {} <<'.format(statement))
            print('[neo4j_connector] Possible issues: ' +
                         '\t Incorrect cypher statement' +
                         '\t Missing packages inside the graph database \n')
            raise Exception('Error in neo4j Connector.')

    def disconnect_driver(self):
        """
        disconnects the connector instance
        @return:
        """
        self.my_driver.close()
        print('Driver disconnected.')

    def check_for_special_characters(self, statement: str) -> bool:
        """
        Checks for special characters which can cause problems for the cypher interpreter
        @param statement: The cypher statement in question
        @return: 'True' if all is good, 'False' if there could be problems, e.g. single quotes within single quotes
        """
        # Get all substrings within curly brackets
        res_list = re.findall(r'\{}.*?\}', statement)

        # iterate over all substrings
        for res in res_list:
            # split the substring by commas
            res = res.split(',')

            # iterate over those substrings
            for item in res:
                # if there are more than 2 single quotes in a substring, there will be a cypher error
                if item.count("'") > 2:
                    return False
        
        # otherwise, return true
        return True
