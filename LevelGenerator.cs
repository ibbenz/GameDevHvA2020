using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

//This Script generates the level at the start of the game.



public enum TileType {
    Empty = 0,
    Player,
    Enemy,
    Wall,
    Door,
    Key,
    Dagger,
    End
}

public class LevelGenerator : MonoBehaviour
{

    //Unfortunately I have not found any Object where this script is added. Therefore I set the values of my variables all here.
    private int splitLevel;
    //How deep the worm should penetrate the nodes.
    //In case of 3, a third of a node should be undermined by the worm. 
    private static int digLevel=3;
    public GameObject[] tiles;

    protected void Start()
    {        
        int width = 64;
        int height = 64;
        //The max. Split-Level is 3 for 64x64, since each node is enclosed by walls and the snake-head has a size of 9

        splitLevel = 3;
        //The direction of the node-split.
        int direction = 0;
        //An Aaray for TileType-Objects. The tiles are placed in a 64x64 field
        //height and width are indices. Each tile has an attribute (Player, Enemy, Wall etc.) which will 
        //enable to fill it later with the according sprites or to implement game mechanics.
        TileType[,] grid = new TileType[height, width];

        //For the ease of programming the whole program works only with a binary split in children-nodes (switch-statement)
        //The ref-Keyword means, that any changes which are made in Methods of the Object will have not only a local but a global 
        //Effect on the grid.
        Node rootNode = new Node(0, 0, width, height, 1,ref grid);

        Random.seed = (int)System.DateTime.Now.Ticks;
        direction = (int)(1.9*Random.value);
        //Debug.Log("Random Direction Int:" + direction);

        //We split the Nodes in a random direction. Each node knows the grid he belongs to.

        FillBlock(grid, 0, 0, width, height, TileType.Wall);
        //Starting point is 26/26 from there a 12/12-Square is left empty
        //FillBlock(grid, 5, 5, 12, 12, TileType.Empty);
        //FillBlock(grid, 26, 26, 12, 12, TileType.Empty);

        //The node split has to occur after the Wall, since the wall fills 
        //the whole playground
        //Root node with "ref"-Prefix, otherwise Attributes will not be changed.
        rootNode.CreateNodeSplit(direction, splitLevel, ref rootNode);
        rootNode.ConnectNodes(ref rootNode.nodeChilds[0], ref rootNode.nodeChilds[1], ref grid,splitLevel);
        rootNode.PositionTargetPlayer(ref grid, splitLevel, ref rootNode);
        //Debug.Log("Connect 0/0:" + rootNode.nodeChilds[0].nodeChilds[0].connectTilePosition);
        //Debug.Log("Connect 0/1:" + rootNode.nodeChilds[0].nodeChilds[1].connectTilePosition);
        //Debug.Log("Connect 1/0:" + rootNode.nodeChilds[1].nodeChilds[0].connectTilePosition);
        //Debug.Log("Connect 1/1:" + rootNode.nodeChilds[1].nodeChilds[1].connectTilePosition);

        //At the position 32/28 one tile is reserved for the Player. 
        //FillBlock(grid, 32, 28, 1, 1, TileType.Player);
        //FillBlock(grid, 30, 30, 1, 1, TileType.Dagger);
        //FillBlock(grid, 34, 30, 1, 1, TileType.Key);
        //FillBlock(grid, 32, 32, 1, 1, TileType.Door);
        //FillBlock(grid, 32, 36, 1, 1, TileType.Enemy);
        //FillBlock(grid, 32, 34, 1, 1, TileType.End);

        Debugger.instance.AddLabel(32, 26, "Room 1");

        //use 2d array (i.e. for using cellular automata)
        CreateTilesFromArray(grid);


    }

    //fill part of array with tiles. Each tile receives an attribute, which tells the tile, what to be filled in.
    private void FillBlock(TileType[,] grid, int x, int y, int width, int height, TileType fillType) {
        for (int tileY=0; tileY<height; tileY++) {
            for (int tileX=0; tileX<width; tileX++) {

                //its better to replace this by a function
                grid[tileY + y, tileX + x] = fillType;
            }
        }
    }

    //use array to create tiles
    private void CreateTilesFromArray(TileType[,] grid) {
        int height = grid.GetLength(0);
        int width = grid.GetLength(1);
        for (int y=0; y<height; y++) {
            for (int x=0; x<width; x++) {
                 TileType tile = grid[y, x];
                //The tiles are filled according to their tile-ID
                 if (tile != TileType.Empty) {
                     CreateTile(x, y, tile);
                 }
            }
        }
    }

    //create a single tile
    private GameObject CreateTile(int x, int y, TileType type) {
        int tileID = ((int)type) - 1;
        if (tileID >= 0 && tileID < tiles.Length)
        {
            GameObject tilePrefab = tiles[tileID];
            if (tilePrefab != null) {
                //The Tile is filled with the Object
                GameObject newTile = GameObject.Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity);
                newTile.transform.SetParent(transform);
                return newTile;
            }

        } else {
            Debug.LogError("Invalid tile type selected");
        }

        return null;
    }


    //********************************************************************************************
    //This is the Node-Class for Node Objects. It contains a Node-Split-Method which enables us to 
    //Create childnodes from the parent node. These child nodes are in this script more (+1) or less (-1) 
    //half the size of the parent node.

        //The number of splits of the parent-node is predefined by the split-level.
        //After the parent-node has been split up according to the split levels, the 
        //each Leave of the Node-Tree is filled with a playground-subobject.

        //For this the FillBlock for the walls and the empty spaces are placed in a separate function, where 
        //The filling-procedure is defined.

    public class Node {
        public int x;
        public int y;
        public int width;
        public int height;
        public int numChildren;
        public int splitLevel;
        public TileType[,] grid;
        //The number of children of a node.
        //The children of the node.
        public Node[] nodeChilds;

        //The List of empty Tiles in a node
        public ArrayList emptyTiles;
        //The position of the empty tile in the node from which the connection to 
        //the neighbouring tile is launched.
        public Vector2 connectTilePosition;

        //A rootnode. If the value of the grid
        public Node(int xin, int yin, int widthin, int heightin, int splitLevelIn, ref TileType[,] gridIn)
        {
            x = xin;
            y = yin;
            width = widthin;
            height = heightin;
            splitLevel = splitLevelIn;
            grid = gridIn;
            //If I define two children (2) the array-range has to be 0-->1.
            nodeChilds = new Node[2];
            emptyTiles = new ArrayList();
            connectTilePosition = new Vector2(100, 100);
        }

        //Methods of the Node-Class

        //The split shall occur along a line. 
        //If the split level is >1 then the function is recursively called again.
        //At the End of the NodeSplit adjacent nodes will be connected.
        public void CreateNodeSplit(int directionIn, int splitLevelIn, ref Node parentNode)
        {
            //Horizontal split=0
            //Vertical split=1
            //The position of the parentnode is needed in order to calculate the position
            //of the childnodes.
            //We will split the nodes according to the split level. If the split level is 1. We will
            //split the playfield only once and fill the nodes with walls and empty tiles.
            //For a first try, there shall be walls at the edges of the node field and empty tiles 
            //in its centre.

            //since the tiles are indices we have to take care, that in case of a division
            //the rounding is done correctly.

            //For the case, that a node will be divided horizontally, the two nodes will have a 
            //new height. Similar with the width in case of a vertical split

            int height1; 
            int height2;

            int width1;
            int width2;

            //The declaration of the childnodes, if there's a split-level>=1;
            Node childNode1;
            Node childNode2;

            //horizontal split
            if (parentNode.height % 2 == 0)
            {
                height1 = parentNode.height / 2;
                height2 = height1;
            }
            else
            {   //We make an even number out of an odd number and split it by 2.
                height1 = (parentNode.height + 1) / 2;
                height2 = (parentNode.height - 1) / 2;
            }

            //vertical split
            if ( width% 2 == 0)
            {
                width1 = parentNode.width / 2;
                width2 = width1;
            }
            else
            {
                width1 = (parentNode.width + 1) / 2;
                width2 = (parentNode.width - 1) / 2;
            }




            if (splitLevelIn > 0)
            {

                //If the splitLevel is >1 then each node is split again.
                //Therefore we randomly define in which direction the split should occur.
                Random.seed = (int)System.DateTime.Now.Ticks;
                directionIn = (int)(1.9 * Random.value);



                switch (directionIn)
                {
                    
                    case 0:
                        //the playfield is horizontally split in two nodes
                        //Debug.Log("Node Split horizontal:");
                        childNode1 = new Node(parentNode.x, parentNode.y, parentNode.width, height1, splitLevelIn - 1,ref grid);
                        //Since the splitLevel is >1 we call another Nodesplit:
                        childNode1.CreateNodeSplit(directionIn, childNode1.splitLevel, ref childNode1);
                        childNode2 = new Node(parentNode.x, parentNode.y + height1, parentNode.width, height2, splitLevelIn - 1,ref grid);
                        childNode2.CreateNodeSplit(directionIn, childNode2.splitLevel, ref childNode2);
                        //And add the nodes as children to the parent node:
                        parentNode.nodeChilds[0] = childNode1;
                        parentNode.nodeChilds[1] = childNode2;
                        //After the split of the nodes ended:
                        //The list of the empty Tiles of the parent node is the combination of the lists of the 
                        //empty Tiles of the child-nodes
                        emptyTiles.AddRange(parentNode.nodeChilds[0].emptyTiles);
                        emptyTiles.AddRange(parentNode.nodeChilds[1].emptyTiles);
    
                        break;
                    case 1:
                        //The playfield is vertically split in two nodes.
                        //Debug.Log("Node Split vertical:");
                        childNode1 = new Node(parentNode.x, parentNode.y, width1, parentNode.height, splitLevelIn - 1,ref grid);
                        //Since the splitLevel is >1 we call another Nodesplit:
                        childNode1.CreateNodeSplit(directionIn, childNode1.splitLevel, ref childNode1);
                        childNode2 = new Node(parentNode.x + width1, parentNode.y, width2, parentNode.height, splitLevelIn - 1,ref grid);
                        childNode2.CreateNodeSplit(directionIn, childNode2.splitLevel, ref childNode2);
                        //and add the nodes as children to the parent node:
                        parentNode.nodeChilds[0] = childNode1;
                        parentNode.nodeChilds[1] = childNode2;
                        //After the split of the nodes ended:
                        //The list of the empty Tiles of the parent node is the combination of the lists of the 
                        //empty Tiles of the child-nodes
                        emptyTiles.AddRange(parentNode.nodeChilds[0].emptyTiles);
                        emptyTiles.AddRange(parentNode.nodeChilds[1].emptyTiles);
                        break;
                }
            }
            else {
                //If there is no node-split anymore (split-level=0) then we will Fill the Node with tiles.
                //Debug.Log("End Split:");
                FillLeaveofTree(x, y, width, height);
            }

            //Here we create the connect tile:
            parentNode.TileForConnect();

        }

        //The position of the connection Tile should be close to the other Node with which we want to connect.
        public void TileForConnect()
        {
            //random value for the connectTile of the parentNode
            int seed;
            int randomCheck = (int)(emptyTiles.Count * Random.value);
            seed = Random.Range(int.MinValue, int.MaxValue);
            Random.InitState(seed);
            randomCheck = (int)(emptyTiles.Count * Random.value);
            connectTilePosition = (Vector2)emptyTiles[randomCheck];
        }


            //fill Node with Empty tiles. Each tile receives an attribute, which tells the tile, what to be filled in.
            private void FillNodeBlock(ref TileType[,] grid, int x, int y, int width, int height)
        {
            Vector2 emptyVector;
            TileType fillType = TileType.Empty;
            //for (int tileY = 0; tileY < height; tileY++)
            //{
            //    for (int tileX = 0; tileX < width; tileX++)
            //    {

            //        //its better to replace this by a function
            //        //Debug.Log("Fill Grid:");
            //        grid[tileY + y, tileX + x] = fillType;
            //    }
            //}

            //A variable for the random-seed.
            int seed;
            int randomCheck;
            //The worm which dugs the chambers has a Random-Movement (Brown-Movement)   
            int seedWorm = Random.Range(int.MinValue, int.MaxValue);
            //How intense the worm should dig caves into the node.
            int digIterations=(width*height)/digLevel;
            //the counter to supervise the dig-Process.
            int digCounter = 0;

            //The position of the wormHead.
            Vector2 wormHead;
            //The random unity direction Vector at the start of the movement.
            Vector2 unitRandVec;
            //The change of the direction-Vector every digIteration.
            Vector2 unitRandVecChange=new Vector2(0,0);
            Random.InitState(seedWorm);
            unitRandVec=Random.insideUnitCircle;

            //The random Start-Point for the Wormhead is in the centre of the Node
            wormHead = new Vector2((x + width / 2), (y + height / 2));

            //Bad cases

            //while (digCounter < digIterations) {
             while (digCounter < digIterations)
            {

                if (((wormHead.y > (y+1)) && (wormHead.y < (y + height-1)))&&((wormHead.x > (x+1)) && (wormHead.x < (x + width-1)))){

                    for(int k = -1; k < 1; k++)
                    {
                        for(int j = -1; j < 1; j++)
                        {
                            grid[(int)wormHead.y+k, (int)wormHead.x+j] = fillType;
                        }
                    }
                    //Debug.Log("WormheadX: " + (int)wormHead.x);
                    //Debug.Log("WormheadY: " + (int)wormHead.y);
                    //Debug.Log("NodeGrid Fill:" + grid[(int)wormHead.y, (int)wormHead.x]);
                }
                else {
                    //The Wormhead shall be reflected, when it hits the boundary of the node.
                    if (wormHead.y <= (y + 1)) {
                        unitRandVec.y = -unitRandVec.y;
                    }

                    if (wormHead.y >= (y + height - 1))
                    {
                        unitRandVec.y = -unitRandVec.y;
                    }

                    if (wormHead.x <= (x + 1))
                    {
                        unitRandVec.x = -unitRandVec.x;
                    }

                    if (wormHead.x >= (x + width-1))
                    {
                        unitRandVec.x = -unitRandVec.x;
                    }

                }
                
                seedWorm = Random.Range(int.MinValue, int.MaxValue);
                Random.InitState(seedWorm);
                unitRandVecChange= Random.insideUnitCircle;
                //The Wormdirection should change only by 10% every diggeration.
                //Debug.Log("UnitRandVec before" + unitRandVec);
                //unitRandVec = unitRandVec + unitRandVecChange/10;
                //Debug.Log("UnitRandVec after" + unitRandVec);
                wormHead.x = wormHead.x + unitRandVec.x;
                wormHead.y = wormHead.y + unitRandVec.y;
                digCounter++;
            }


            for (int tileY = 0; tileY < height; tileY++)
            {
                for (int tileX = 0; tileX < width; tileX++)
                {

                    //its better to replace this by a function
                    //Debug.Log("Fill Grid:");
                    //grid[tileY + y, tileX + x] = fillType;

                    //emptyVector = new Vector2(tileX + x, tileY + y);
                    //Debug.Log("NodeGrid IF X:" + (tileX + x));
                    //Debug.Log("NodeGrid IF Y:" + (tileY + y));
                    //Debug.Log("NodeGrid IF:"+ grid[tileY + y, tileX + x]);
                    
                   

                    if (grid[tileY + y, tileX + x] == fillType)
                    {
                        emptyVector = new Vector2(tileX + x, tileY + y);

                        //Here the Empty tiles of the node are filled in a Vector
                        emptyTiles.Add(emptyVector);
                    }

                    //Here the Empty tiles of the node are filled in a Vector
                    //emptyTiles.Add(emptyVector);
                    //Debug.Log("Grid Value:"+ grid[tileY + y, tileX + x]);
                }
            }





            //Add all empty tiles to an Array for the node

            //Here we choose the Empty-Tile-Position, from which the connection 
            //to the neighbour-node should be established.
            seed = Random.Range(int.MinValue, int.MaxValue);
            Random.InitState(seed);
            randomCheck = (int)(emptyTiles.Count * Random.value);
            connectTilePosition = (Vector2)emptyTiles[randomCheck];
            //Debug.Log("Random Empty Tile x:"+ connectTilePosition.x);
            //Debug.Log("Random Empty Tile y:" + connectTilePosition.y);
        }


        //Here's the method which will fill the node, in case there is no further split possible
        private void FillLeaveofTree(int xInLeafe, int yInLeafe, int widthInLeafe, int heightInLeafe)
        {
            //Debug.Log("Fill Leave:");
            //In a first attempt we define the first/last row/column as walls and the other parts as empty.
            FillNodeBlock(ref grid, (xInLeafe+1), (yInLeafe+1), (widthInLeafe-2), (heightInLeafe-2));
            //FillNodeBlock(ref grid, 26, 26, 12,12,TileType.Empty);
        }

        //This method connects two neighbouring nodesk
        //We tell the method which two nodes have to be connected.
        public void ConnectNodes(ref Node Node1In, ref Node Node2In, ref TileType[,] grid, int splitLevelIn)
        {
            //The Start and End-Tile of the connection:
            Vector2 emptyNode1In = Node1In.connectTilePosition;
            //Debug.Log("connectNode1x:" + emptyNode1In.x);
            //Debug.Log("connectNode1y:" + emptyNode1In.y);
            Vector2 emptyNode2In = Node2In.connectTilePosition;
            //Debug.Log("connectNode2x:" + emptyNode2In.x);
            //Debug.Log("connectNode2y:" + emptyNode2In.y);

            //The simplest way to connect two nodes is by a straight line. This line has to have a 
            //thickness of at least 2 tiles, otherwise the player can't move.

            Vector2 diffVec = emptyNode2In - emptyNode1In;
            Vector2 normalDiffVec = diffVec.normalized;
            //The normal Vector to the expanding connection.
            Vector2 normalDiffVec2 = new Vector2(normalDiffVec.y,-normalDiffVec.x);
            float lengthDiffVec = diffVec.magnitude;

            //The temporary Vector which is used to connect the two Nodes step by step.
            Vector2 tempVecFloat = new Vector2(0, 0);
            //The temporary Vector to calculate the positions left and right of the expanding connection.
            Vector2 tempVecFloat2 = new Vector2(0, 0);
            Vector2 tempVecFloat3 = new Vector2(0, 0);

            Vector2 tempVecRound = new Vector2(0, 0);
            //To check if there is an Empty Tile left or right of the expanding connection and belongs to the other node.
            Vector2 tempVecRound2 = new Vector2(0, 0);
            Vector2 tempVecRound3 = new Vector2(0, 0);
            //The array in which the float values are rounded to int-Values
            int[] tempVec = new int[2];
            //To check if the tiles on the left and right of the expanding connection are Empty and belonging to the other node.
            int[] tempVec2 = new int[2];
            int[] tempVec3 = new int[2];


            //The criteria when we can stop connecting the nodes.
            int traceEnd = 0;
            //The counter is counted up, as long as the connection has not yet reached the empty area of the other node.
            //The counter is a double value, so that the normalDiffVec is not casted to int when multiplied.
            float counter = 1;

            while (traceEnd == 0)
            {

                tempVecFloat2= emptyNode1In + (counter-1) * normalDiffVec+ normalDiffVec2;
                tempVecFloat3 = emptyNode1In + (counter - 1) * normalDiffVec - normalDiffVec2;

                tempVecFloat = emptyNode1In + counter * normalDiffVec;
                //Due to the cast the values will be rounded to the next lower integer.
                tempVec[0] = (int)tempVecFloat.x;
                tempVec[1] = (int)tempVecFloat.y;
                tempVecRound.x = tempVec[0];
                tempVecRound.y = tempVec[1];

                //The node normal left to the expanding connection
                tempVec2[0] = (int)tempVecFloat2.x;
                tempVec2[1] = (int)tempVecFloat2.y;
                tempVecRound2.x = tempVec2[0];
                tempVecRound2.y = tempVec2[1];

                //The node normal right to the expanding connection.
                tempVec3[0] = (int)tempVecFloat3.x;
                tempVec3[1] = (int)tempVecFloat3.y;
                tempVecRound3.x = tempVec3[0];
                tempVecRound3.y = tempVec3[1];

                //If the expanding connection from node1 meets a empty field of node2, then finish the expansion, and 
                //set a Door.
                if (Node2In.emptyTiles.Contains(tempVecRound)|| Node2In.emptyTiles.Contains(tempVecRound2) || Node2In.emptyTiles.Contains(tempVecRound3))
                {
                    //If we have come from node1 to node2 then stop the connection
                    //Due to the cast the values will be rounded to the next lower integer.
                    grid[tempVec[1], tempVec[0]] = TileType.Empty;
                    if ((tempVec[0] - (int)(Mathf.Round(normalDiffVec.x)) < width) && (tempVec[0] - (int)(Mathf.Round(normalDiffVec.x)) > 0))
                    {
                        //grid[tempVec[1], tempVec[0] - (int)(Mathf.Round(normalDiffVec.x))] = TileType.Dagger;
                        grid[tempVec[1], tempVec[0] - (int)(Mathf.Round(normalDiffVec.x))] = TileType.Empty;
                    }

                    if ((tempVec[1] - (int)(Mathf.Round(normalDiffVec.y)) < height) && (tempVec[1] - (int)(Mathf.Round(normalDiffVec.y)) > 0))
                    {
                        //grid[tempVec[1] - (int)(Mathf.Round(normalDiffVec.y)), tempVec[0]] = TileType.Dagger;
                        grid[tempVec[1] - (int)(Mathf.Round(normalDiffVec.y)), tempVec[0]] = TileType.Empty;
                    }


                    //Here we set the Doors:
                    grid[tempVec[1] - (int)(2 * Mathf.Round(normalDiffVec.y)), tempVec[0] - (int)(2 * Mathf.Round(normalDiffVec.x))] = TileType.Door;
                    grid[tempVec[1] - (int)(2 * Mathf.Round(normalDiffVec.y)), tempVec[0] - (int)(3 * Mathf.Round(normalDiffVec.x))] = TileType.Door;
                    grid[tempVec[1] - (int)(3 * Mathf.Round(normalDiffVec.y)), tempVec[0] - (int)(2 * Mathf.Round(normalDiffVec.x))] = TileType.Door;
                    traceEnd = 1;

                }
                else
                {
                    //else the tile and its surrounding shall be converted to empty
                    //as long as they are within range.

                    //Debug.Log("Y-Position:" + tempVec[1]);
                    //Debug.Log("X-Position:" + tempVec[0]);
                    //grid[tempVec[1], tempVec[0]] = TileType.Empty;
                    //grid[tempVec[1], tempVec[0]-(int)(counter * normalDiffVec.x)] = TileType.Empty;
                    //grid[tempVec[1]- (int)(counter * normalDiffVec.y), tempVec[0]] = TileType.Empty;


                    //In order to follow the path, the Empty should be replaced by the dagger Dagger
                    if (((tempVec[0] < 0) || (tempVec[1]) < 0)|| ((tempVec[0] > (width - 1)) || (tempVec[1]) > (height - 1)))
                    {
                        traceEnd = 1;
                    } else {
                        //grid[tempVec[1], tempVec[0]] = TileType.Dagger;
                        grid[tempVec[1], tempVec[0]] = TileType.Empty;
                    }

                    if ((tempVec[0] - (int)(Mathf.Round(normalDiffVec.x)) < width) && (tempVec[0] - (int)(Mathf.Round(normalDiffVec.x)) > 0))
                    {
                        //grid[tempVec[1], tempVec[0] - (int)(Mathf.Round(normalDiffVec.x))] = TileType.Dagger;
                        grid[tempVec[1], tempVec[0] - (int)(Mathf.Round(normalDiffVec.x))] = TileType.Empty;
                    }
                    if ((tempVec[1] - (int)(Mathf.Round(normalDiffVec.y)) < height) && (tempVec[1] - (int)(Mathf.Round(normalDiffVec.y)) > 0))
                    {
                        //grid[tempVec[1] - (int)(Mathf.Round(normalDiffVec.y)), tempVec[0]] = TileType.Dagger;
                        grid[tempVec[1] - (int)(Mathf.Round(normalDiffVec.y)), tempVec[0]] = TileType.Empty;
                    }


                    //***************************************************************************************


                    counter++;

                    //Here we set the doors within the 
                }
            }

            //If we are on the level of a parent nodes, then, after we connected the parent nodes,
            //we will connect the child nodes too.
            //If we are already in a child node, we don't have to do this step, since there are no childNodes (leaves)
            if (splitLevelIn > 1)
            {
                
                ConnectNodes(ref Node1In.nodeChilds[0], ref Node1In.nodeChilds[1], ref grid, (splitLevelIn - 1));
                ConnectNodes(ref Node2In.nodeChilds[0], ref Node2In.nodeChilds[1], ref grid, (splitLevelIn - 1));
            }
        }
        //End of ConnectNodes


        //Choose node for player and target. This method is basically only used for the root-node!
        public void PositionTargetPlayer(ref TileType[,] grid, int splitLevelIn, ref Node parentNode)
        {
            Node targetNode = parentNode;
            Node playerNode = parentNode;
            //Node to be filled with Daggers, Key and an Enemy.
            //If there is no player or a target in the node, they can be placed 
            //equally distributed.
            Node nodeToFill = parentNode;
            Vector2 playerPosition;
            Vector2 targetPosition;

            for (int i = 0; i < splitLevelIn; i++)
            {
                //the utmost left node in the tree is the target node.
                targetNode = targetNode.nodeChilds[0];
                //the utmost right node in the tree is the playernode.
                playerNode = playerNode.nodeChilds[1];
            }

            playerPosition = (Vector2)playerNode.emptyTiles[0];
            //Debug.Log("PlayerPosition: " + playerPosition);
            targetPosition = (Vector2)targetNode.emptyTiles[0];
            //Debug.Log("TargetPosition: " + targetPosition);

            // Place Player
            grid[(int)playerPosition.y, (int)playerPosition.x] = TileType.Player;
            grid[(int)targetPosition.y, (int)targetPosition.x] = TileType.End;

            //After we set player and target we now
            //place the Dagger, Enemy and Keys in the Nodes.
            parentNode.GoThroughNodes(ref grid, parentNode, playerNode, targetNode, splitLevelIn);

        }

        //A method, which enables to roam trough all the nodes of a Tree and check
        //if there is a player or not in the node. If there is no player in the node, then 
        //an enemy a dagger and 3 keys are placed in the node.
        public void GoThroughNodes(ref TileType[,] gridIn, Node parentNode, Node playerNodeIn, Node targetNodeIn, int splitLevelIn) {

            //As long as we aren't on the "Leaves"-Level of the nodes,
            //we repeat the GoTroughNodes.
            if (splitLevelIn > 0)
            {
                parentNode.GoThroughNodes(ref gridIn,parentNode.nodeChilds[0], playerNodeIn,targetNodeIn, (splitLevelIn - 1));
                parentNode.GoThroughNodes(ref gridIn,parentNode.nodeChilds[1], playerNodeIn,targetNodeIn, (splitLevelIn - 1));

            }

            

            else {
                //We are not on the Leaf-Level of the Nodes. 
                //If the node is not the node wherein the player is placed, we just fill it with 
                //an enemy, a dagger and three keys.
                Vector2 position = (Vector2)parentNode.emptyTiles[0];
                //the first empty tile is always the snakehead, where we start the snake.
                //We now distribute the dagger, the enemy and the keys equally over the empty tiles. (totally 5 items)
                int numEmptyTiles = (parentNode.emptyTiles.Count - 1);
                Debug.Log("NumEmptyTiles: " + numEmptyTiles);
                //Two enemies and two daggers in every cave, if it's big enough.
                int numberOfEnemies = 4;
                int enemyDensity = numEmptyTiles / numberOfEnemies;

                if ((parentNode != playerNodeIn) && (parentNode != targetNodeIn))
                {
                    //In case that the distribution is smaller than 1 set the distribute to 1 (3 keys, 1 enemy, 1 dagger)
                    //The keys shall be at the end of the tunnel (snakehead)
                    //The enemy is placed just before the keys
                    //Finally the dagger follows.

                    if (enemyDensity > 5) { 
                    for (int i = 1; i < numEmptyTiles; i = i + enemyDensity)
                        {
                        position = (Vector2)parentNode.emptyTiles[i];
                        gridIn[(int)position.y, (int)position.x] = TileType.Enemy;
                        }

                    for (int i = (1 + (enemyDensity / 2)); i < numEmptyTiles; i = i + enemyDensity)
                        {
                        position = (Vector2)parentNode.emptyTiles[i];
                        gridIn[(int)position.y, (int)position.x] = TileType.Dagger;
                        }
                    }
                    else
                    {
                        position = (Vector2)parentNode.emptyTiles[enemyDensity];
                        gridIn[(int)position.y, (int)position.x] = TileType.Enemy;
                        //only an enemy in a small cave:
                    }

                    position = (Vector2)parentNode.emptyTiles[0];
                    gridIn[(int)position.y, (int)position.x] = TileType.Key;
                    position = (Vector2)parentNode.emptyTiles[0+enemyDensity];
                    gridIn[(int)position.y, (int)position.x] = TileType.Key;
                    position = (Vector2)parentNode.emptyTiles[0 + 2*enemyDensity];
                    gridIn[(int)position.y, (int)position.x] = TileType.Key;
                }
                else
                {
                    //The player and the target are always at index[0]
                    //The enemy is placed far away.
                    if (parentNode == playerNodeIn){
                        int positioning = numEmptyTiles / 4;
                        position = (Vector2)parentNode.emptyTiles[positioning];
                        gridIn[(int)position.y, (int)position.x] = TileType.Enemy;
                        gridIn[(int)position.y+1, (int)position.x+1] = TileType.Key;
                        position = (Vector2)parentNode.emptyTiles[2*positioning];
                        gridIn[(int)position.y, (int)position.x] = TileType.Dagger;
                        gridIn[(int)position.y+1, (int)position.x+1] = TileType.Key;
                        position = (Vector2)parentNode.emptyTiles[3*positioning];
                        gridIn[(int)position.y, (int)position.x] = TileType.Key;
                    }


                    //if it's the target node, no keys are necessary anymore
                    if (parentNode == targetNodeIn)
                    {
                        if (enemyDensity > 5)
                        {
                            for (int i = enemyDensity; i < numEmptyTiles; i = i + enemyDensity)
                            {
                                position = (Vector2)parentNode.emptyTiles[i];
                                gridIn[(int)position.y, (int)position.x] = TileType.Enemy;
                            }

                            for (int i = (enemyDensity + (enemyDensity / 2)); i < numEmptyTiles; i = i + enemyDensity)
                            {
                                position = (Vector2)parentNode.emptyTiles[i];
                                gridIn[(int)position.y, (int)position.x] = TileType.Dagger;
                            }
                        }
                        else
                        {
                            position = (Vector2)parentNode.emptyTiles[enemyDensity];
                            gridIn[(int)position.y, (int)position.x] = TileType.Enemy;
                            //only an enemy in a small cave:
                        }
                    }




                }
            }
        }
    }
    //End of Class Node
}
