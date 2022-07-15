import csv
from typing import List

from neo4jInterface.EdgeItem import EdgeItem
from neo4jInterface.NodeItem import NodeItem


class AccessibilityExtractor:

    def __init__(self):
        pass

    @staticmethod
    def write_to_csv(nodes: List[NodeItem], edges: List[EdgeItem]):

        header = ["Graph ID", "Nodes for Steller Graph", "Space", "Door", "Stairs", "Ramp", "170", "91.5", "81.5",
                  "Corridor or not", "Ramp Slope", "Fake Labels"]
        data = []
        graph_id = 1

        for node in nodes:

            # encode component type
            type_switcher = {
                "Room": [1, 0, 0, 0],
                "Door": [0, 1, 0, 0],
                "Stair": [0, 0, 1, 0],
                "Ramp": [0, 0, 0, 1]
            }

            if node.labels[0] not in type_switcher:
                raise Exception("Node Label was not specified for the requested extractor. ")

            type_encoded = type_switcher[node.labels[0]]

            # encode width
            width_switcher = {
                170: [1, 1, 1],
                91.5: [0, 1, 1],
                81.5: [0, 0, 1]
            }

            width_encoded = [0, 0, 0]

            if "Width" in node.attrs:
                width = node.attrs["Width"]

                if width >= 1.70:
                    width_encoded = width_switcher[170]
                elif width >= 0.915:
                    width_encoded = width_switcher[91.5]
                elif width >= 0.815:
                    width_encoded = width_switcher[81.5]

            # corridor yes or no?
            is_corridor = 0

            # ramp slope
            slope = 0

            # fake labels
            label = "empty"

            row = [graph_id, node.id, *type_encoded, *width_encoded, is_corridor, slope, label]
            data.append(row)

        with open('MiniSample_TUM_nodes.csv', 'w', encoding='UTF8', newline='') as f:
            writer = csv.writer(f)

            # write the header
            writer.writerow(header)

            # write multiple rows
            writer.writerows(data)
