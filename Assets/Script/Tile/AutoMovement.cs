using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AutoMovement : MonoBehaviour
{
    public GameObject tilePrefab;
    public GameObject robotPrefab;
    public GameObject sliderPrefab;

    Grid3d _grid;
    
    GameObject _tile;    
    GameObject _robot1;
    GameObject _robot2;

    Vector3 tilePosOrigin;
    Vector3 tilePosTemp;
    Vector3 tileTargetPosTemp;

    Vector3 robotPos;
    Vector3 robotTempPos;

    Vector3 boxSize = new Vector3(1, 0.2f, 1);
    Vector3 pivotRobot;
    Vector3 pivotTile;

    public int stackIndex;
    public static int tileNumber;

    int stepX;
    int stepY;
    int stepZ;

    
    Vector3 directionX;
    Vector3 directionY;
    Vector3 directionZ;
    

    public static List<GameObject> _pickTiles = new List<GameObject>();
    public static List<GameObject> _pickRobots = new List<GameObject>();

    void Start()
    {
        tilePosOrigin = this.gameObject.transform.position;
    }

    public void SpawnPickTile(int number)
    {
        int m = number / 50;
        int n = number % 50;
        int max;

        for (int i = 0; i < m; i++)
        {
            if (i < m) max = 50;
            else max = n;

            for (int j = 0; j < max; j++)
            {
                var pickTilePos = new Vector3(this.transform.position.x - 2 + 0.4f - j * 0.2f, 0.5f + i, this.transform.position.z);
                var pickTile = Instantiate(tilePrefab, pickTilePos, Quaternion.Euler(0, 0, 90));
                pickTile.transform.SetParent(this.transform);
                pickTile.tag = "PickTile";
                _pickTiles.Add(pickTile);
            }
        }
    }

    public void SpawnPickRobot()
    {
        var pickRobotPos1 = new Vector3(this.transform.position.x - 2 + 0.6f, 0.5f, this.transform.position.z);
        _robot2 = Instantiate(robotPrefab, pickRobotPos1, Quaternion.Euler(0, 0, 90));
        _robot2.transform.SetParent(this.transform);

        var pickRobotPos2 = new Vector3(this.transform.position.x - 2 + 0.6f, 1.5f, this.transform.position.z);
        _robot1 = Instantiate(robotPrefab, pickRobotPos2, Quaternion.Euler(0, 0, 90));
        _robot1.transform.SetParent(this.transform);

        _pickRobots.Add(_robot2);
        _pickRobots.Add(_robot1);

        var slider = Instantiate(sliderPrefab, this.transform.position + Vector3.down * 0.09f, Quaternion.Euler(90, 0, 0));
        slider.transform.SetParent(this.transform);

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            StartCoroutine(BuildManualMode());
        }
        
        if (Input.GetKeyDown(KeyCode.L))
        {
            StartCoroutine(BuildAutoMode());
        }

    }

    
    public IEnumerator BuildAutoMode()
    {
        for (int j = 1; j < 12; j++)
        {
            int h = 12/j;
            for (int i = 0; i < h; i++)
            {
                int stackCount = CreateStack._pickingStacks.Count;

                Vector3 targetPos;

                if (stackIndex <= stackCount / 2) targetPos = new Vector3(-0.5f*j, 0.1f + 0.6f * i, -stackCount / 4 + stackIndex - 0.5f);
                else targetPos = new Vector3(0.5f*j, 0.1f + 0.6f * i, -stackCount / 4 - stackCount / 2 + stackIndex - 0.5f);
                StartCoroutine(Reset(targetPos, Quaternion.identity, 8));
                yield return new WaitForSeconds((stepX + stepY + stepZ) * 0.5f + 3f);
            }
        }
        
    }

    public IEnumerator BuildManualMode()
    {
        for (int i = 0; i < RaycastCreate.tileList.Count; i++)
        {
            var _target = RaycastCreate.tileList[i];

            Destroy(_pickTiles[_pickTiles.Count - 1]);
            _pickTiles.RemoveAt(_pickTiles.Count - 1);

            StartCoroutine(Reset(_target.transform.position, _target.transform.rotation, _target.layer));
            yield return new WaitForSeconds((stepX + stepY+ stepZ)* 0.5f + 3f);
        }        
    }

    public void Spawn(Vector3 offset, float robotOffset)
    {
        tilePosTemp = tilePosOrigin + offset;
        robotPos = new Vector3(tilePosTemp.x + robotOffset, boxSize.y / 2, tilePosTemp.z);
        _tile = Instantiate(tilePrefab, tilePosTemp, Quaternion.identity);
        _robot1.transform.position = robotPos;
        _robot1.transform.rotation = Quaternion.identity;
    }

    float tileMoveDuration;

    public IEnumerator Reset(Vector3 targetPos, Quaternion targetRot, int layer)
    {
        tileTargetPosTemp = targetPos;

        stepX = Mathf.Abs(Mathf.RoundToInt(tileTargetPosTemp.x - tilePosOrigin.x) * 2);
        stepY = Mathf.Abs((int)(tileTargetPosTemp.z - tilePosOrigin.z) * 2);
        stepZ = Mathf.RoundToInt((tileTargetPosTemp.y - tilePosOrigin.y) / 0.6f);


        if (this.gameObject.transform.rotation == Quaternion.identity)
        {
            if (((stepX + stepY) / 2) % 2 == 0)
            {
                Spawn(Vector3.zero, 1);
            }
            else if (((stepX + stepY) / 2) % 2 == 1)
            {
                Spawn(Vector3.left, 1);
            }
        }
        else
        {
            if (((stepX + stepY) / 2) % 2 == 0)
            {
                Spawn(Vector3.zero, -1);
            }
            else if (((stepX + stepY) / 2) % 2 == 1)
            {
                Spawn(Vector3.right, -1);
            }
        }

        float pivotOffsetX = boxSize.x / 2;
        float pivotOffsetY = boxSize.y;
        float pivotOffsetZ = boxSize.z / 2;

        if (tileTargetPosTemp.z - tilePosOrigin.z >= 0 && tileTargetPosTemp.x - tilePosOrigin.x > 0)
        {
            pivotOffsetX = boxSize.x / 2;
            pivotOffsetY = boxSize.y;
            pivotOffsetZ = boxSize.z / 2;
            directionX = Vector3.back;
            directionZ = Vector3.right;
        }
        else if (tileTargetPosTemp.z - tilePosOrigin.z < 0 && tileTargetPosTemp.x - tilePosOrigin.x > 0)
        {
            pivotOffsetX = boxSize.x / 2;
            pivotOffsetY = boxSize.y;
            pivotOffsetZ = -boxSize.z / 2;
            directionX = Vector3.back;
            directionZ = Vector3.left;
        }
        else if (tileTargetPosTemp.z - tilePosOrigin.z >= 0 && tileTargetPosTemp.x - tilePosOrigin.x < 0)
        {
            pivotOffsetX = -boxSize.x / 2;
            pivotOffsetY = boxSize.y;
            pivotOffsetZ = boxSize.z / 2;
            directionX = Vector3.forward;
            directionZ = Vector3.right;
        }
        else if (tileTargetPosTemp.z - tilePosOrigin.z < 0 && tileTargetPosTemp.x - tilePosOrigin.x < 0)
        {
            pivotOffsetX = -boxSize.x / 2;
            pivotOffsetY = boxSize.y;
            pivotOffsetZ = -boxSize.z / 2;
            directionX = Vector3.forward;
            directionZ = Vector3.left;
        }

        if (stepZ == 0)
        {
            StartCoroutine(MainGridMovement(0, pivotOffsetX, pivotOffsetY, pivotOffsetZ, directionX, directionZ));
            yield return new WaitForSeconds((stepX + stepY) * 0.5f);            
            //StartCoroutine(RobotAutoMovement());
        }
        else
        {
            StartCoroutine(MainGridMovement(1, pivotOffsetX, pivotOffsetY, pivotOffsetZ, directionX, directionZ));
            yield return new WaitForSeconds(tileMoveDuration);

            if (tileTargetPosTemp.x - tilePosOrigin.x > 0)
            StartCoroutine(BuildVertical(stepZ, 0.5f,0.1f,0.5f,Vector3.back,Vector3.left));
            else
            StartCoroutine(BuildVertical(stepZ, -0.5f, -0.1f, 0.5f, Vector3.forward, Vector3.right));

            yield return new WaitForSeconds(stepZ * 0.5f+2);
            //StartCoroutine(RobotAutoMovement());
        }        
    }
  
    IEnumerator Tumble(GameObject instance, Vector3 rotAxis, Vector3 pivot, float angle)
    {
        var rotSpeed = angle / 0.05f;
        var totalRotation = 0f;
        while (totalRotation <= angle)
        {
            var delta = rotSpeed * Time.deltaTime;

            if (totalRotation + delta > angle)
                delta = angle - totalRotation;

            totalRotation += delta;
            if (instance != null)
                instance.transform.RotateAround(pivot, rotAxis, delta);
            yield return null;
        }
    }

    IEnumerator RobotMoveBackGround()
    {
        StartCoroutine(RobotMovement(Vector3.forward, Vector3.forward,-boxSize.x/2, -boxSize.y / 2, -boxSize.z / 2, 90f, stepX - 8, -1));
        yield return new WaitForSeconds(stepX * 0.5f - 3);
        StartCoroutine(RobotMovement(Vector3.left, Vector3.left, -boxSize.x / 2, -boxSize.y / 2, -boxSize.z / 2, 90f, stepY, 0));
        yield break;
    }

    IEnumerator MainGridMovement(int offset,float pivotOffsetX, float pivotOffsetY, float pivotOffsetZ, Vector3 directionX, Vector3 directionZ)
    {     
            int stepDiagonalRough = (int)((Mathf.Sqrt(stepY * stepY + stepY * stepY)) * 1.5);
            int stepDiagonal = stepDiagonalRough - stepDiagonalRough % 4;

            StartCoroutine(MovementHorizontal(directionX, directionZ,pivotOffsetX, pivotOffsetY, pivotOffsetZ, 180f, stepDiagonal, -1));
            yield return new WaitForSeconds(stepDiagonal * 0.5f + 0.5f);

            int stepLinearRough = Mathf.Abs((int)tileTargetPosTemp.x - (int)_tile.transform.position.x);

            int stepLinear;
            if (stepLinearRough % 2 == 0) stepLinear = stepLinearRough * 2 - 2;
            else stepLinear = stepLinearRough * 2;

            StartCoroutine(MovementHorizontal(directionX, directionX,pivotOffsetX, pivotOffsetY, pivotOffsetZ, 180f, stepLinear - offset, 0));

            tileMoveDuration = ((stepDiagonal + stepLinear) * 0.5f + 0.5f);
            yield break;
    }
    
    //Main Movement

    IEnumerator MovementHorizontal(Vector3 rotAxis1, Vector3 rotAxis2, float pivotOffsetX, float pivotOffsetY, float pivotOffsetZ, float angle, int step, int firstStep)
    {
        for (int i = firstStep; i < step; i++)
        {
            if (i % 4 == 0)
            {
                pivotTile = new Vector3(_tile.transform.position.x + pivotOffsetX , pivotOffsetY, _tile.transform.position.z + pivotOffsetZ);
                StartCoroutine(Tumble(_tile, rotAxis1, pivotTile, angle));
                yield return new WaitForSeconds(0.5f);
            }
            else if (i % 4 == 1)
            {
                pivotTile = new Vector3(_tile.transform.position.x + pivotOffsetX, pivotOffsetY, _tile.transform.position.z + pivotOffsetZ);
                StartCoroutine(Tumble(_tile, rotAxis2, pivotTile, angle));
                yield return new WaitForSeconds(0.5f);
            }
            else if (i % 4 == 2)
            {
                pivotRobot = new Vector3(_robot1.transform.position.x + pivotOffsetX, pivotOffsetY, _robot1.transform.position.z + pivotOffsetZ);
                StartCoroutine(Tumble(_robot1, rotAxis2, pivotRobot, angle));
                yield return new WaitForSeconds(0.5f);
            }
            else if (i % 4 == 3)
            {
                pivotRobot = new Vector3(_robot1.transform.position.x + pivotOffsetX, boxSize.y, _robot1.transform.position.z + pivotOffsetZ);
                StartCoroutine(Tumble(_robot1, rotAxis1, pivotRobot, angle));
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    IEnumerator RobotMovement(Vector3 rotAxis1, Vector3 rotAxis2, float pivotOffsetX, float pivotOffsetY, float pivotOffsetZ, float angle, int step, int firstStep)
    {
        for (int i = firstStep; i < step; i++)
        {
            if (i % 4 == 0)
            {
                pivotRobot = new Vector3(_robot1.transform.position.x + pivotOffsetX, 0, _robot1.transform.position.z + pivotOffsetZ);
                StartCoroutine(Tumble(_robot1, rotAxis1, pivotRobot, angle));
                yield return new WaitForSeconds(0.5f);
            }
            else if (i % 4 == 1)
            {
                pivotRobot = new Vector3(_robot1.transform.position.x + pivotOffsetY, 0, _robot1.transform.position.z + pivotOffsetY);
                StartCoroutine(Tumble(_robot1, rotAxis1, pivotRobot, angle));
                yield return new WaitForSeconds(0.5f);
            }
            else if (i % 4 == 2)
            {
                pivotRobot = new Vector3(_robot1.transform.position.x + pivotOffsetX, 0, _robot1.transform.position.z + pivotOffsetZ);
                StartCoroutine(Tumble(_robot1, rotAxis2, pivotRobot, angle));
                yield return new WaitForSeconds(0.5f);
            }
            else if (i % 4 == 3)
            {
                pivotRobot = new Vector3(_robot1.transform.position.x + pivotOffsetY, 0, _robot1.transform.position.z + pivotOffsetY);
                StartCoroutine(Tumble(_robot1, rotAxis2, pivotRobot, angle));
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    IEnumerator MovementVertical(Vector3 rotAxis, float pivotOffsetX, float pivotOffsetY, float pivotOffsetZ, float angle, int step, int firstStep)
    {
        for (int i = firstStep; i < step; i++)
        {
            if (i % 2 == 0)
            {
                pivotTile = new Vector3(_tile.transform.position.x + pivotOffsetX, _tile.transform.position.y + 0.1f, _tile.transform.position.z + pivotOffsetZ);
                StartCoroutine(Tumble(_tile, rotAxis, pivotTile, angle));   
                StartCoroutine(Tumble(_robot1, rotAxis, pivotTile, angle));
                yield return new WaitForSeconds(0.5f);
            }
            else if (i % 2 == 1)
            {
                pivotTile = new Vector3(_tile.transform.position.x + pivotOffsetY, _tile.transform.position.y + 0.5f, _tile.transform.position.z + pivotOffsetZ);
                StartCoroutine(Tumble(_tile, rotAxis, pivotTile, angle));
                StartCoroutine(Tumble(_robot1, rotAxis, pivotTile, angle));
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
    
    // Placing Tile

    IEnumerator PlacingTile(Vector3 rotAxis1, Vector3 rotAxis2, float pivotOffsetX, float pivotOffsetY, float pivotOffsetZ, float angle)
    {
        pivotTile = new Vector3(_tile.transform.position.x + pivotOffsetX, _tile.transform.position.y + pivotOffsetY, _tile.transform.position.z + pivotOffsetZ);
        StartCoroutine(Tumble(_tile, rotAxis1, pivotTile, 180f));
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(Tumble(_tile, rotAxis2, pivotTile, angle));
    }

    IEnumerator PlacingHorizontalVertical(float angle)
    {
        pivotTile = new Vector3(_tile.transform.position.x + boxSize.x / 2, _tile.transform.position.y - boxSize.x / 2, _tile.transform.position.z + boxSize.y / 2);
        StartCoroutine(Tumble(_tile, Vector3.right, pivotTile, 180f));
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(Tumble(_tile, Vector3.up, pivotTile, angle));
    }    

    IEnumerator BuildVertical(int stepVertical, float pivotOffsetX, float pivotOffsetY, float pivotOffsetZ, Vector3 rotAxis1, Vector3 rotAxis2)
    {
        if (stepVertical == 1 )
        {
            StartCoroutine(MovementHorizontal(rotAxis1, rotAxis1, pivotOffsetX, 0.2f, pivotOffsetZ, 90f, 1, 0));
            yield return new WaitForSeconds(0.5f);
        }
        else if (stepVertical != 0 && stepVertical % 2 == 0)
        {
            StartCoroutine(MovementHorizontal(rotAxis2, rotAxis2, pivotOffsetX, 0.2f, pivotOffsetZ, 180f, 1, 0));
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(MovementVertical(rotAxis1, pivotOffsetX, pivotOffsetY, pivotOffsetZ, 90f, stepVertical, 0));
            yield return new WaitForSeconds(stepVertical * 0.5f + 0.5f);
            StartCoroutine(PlacingTile(rotAxis2, rotAxis1, pivotOffsetX, 0.1f, -pivotOffsetZ, 180));
            yield return new WaitForSeconds(1.5f);
        }
        else if (stepVertical != 1 && stepVertical % 2 == 1)
        {
            StartCoroutine(MovementHorizontal(rotAxis2, rotAxis2, pivotOffsetX, 0.2f, pivotOffsetZ, 180f, 1, 0));
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(MovementVertical(rotAxis1, pivotOffsetX, pivotOffsetY, pivotOffsetZ, 90f, stepVertical - 1, 0));
            yield return new WaitForSeconds(stepVertical * 0.5f + 0.5f);
            StartCoroutine(PlacingTile(rotAxis2, rotAxis1, pivotOffsetX, 0.1f, -pivotOffsetZ, 90));
            yield return new WaitForSeconds(1.5f);            
        }
    }

    //int stepBeam = 0;
    //int horizontalStep = 0;
    //IEnumerator BuildBeam()
    //{
    //    //MoveVertical
    //    StartCoroutine(Movement(Vector3.left, Vector3.left, pivotOffset, 180f, 1, 0));
    //    yield return new WaitForSeconds(0.5f);
    //    StartCoroutine(MovementVertical(Vector3.back, Vector3.back, 90f, pivotOffset, verticalStep - 1, 0));
    //    yield return new WaitForSeconds((verticalStep - 1) * 0.5f);

    //    if (stepBeam == 0)
    //    {
    //        pivotTile = new Vector3(_tile.transform.position.x + boxSize.y / 2, _tile.transform.position.y + boxSize.x / 2, _tile.transform.position.z - boxSize.z / 2);
    //        StartCoroutine(Tumble(_tile, Vector3.up, pivotTile, 90f));
    //        yield return new WaitForSeconds(1.0f);
    //    }
    //    else if (stepBeam == 1)
    //    {
    //        pivotTile = new Vector3(_tile.transform.position.x - boxSize.y / 2, _tile.transform.position.y + boxSize.x / 2, _tile.transform.position.z - boxSize.z / 2);
    //        StartCoroutine(Tumble(_tile, Vector3.down, pivotTile, 180f));
    //        yield return new WaitForSeconds(0.5f);
    //        pivotRobot = new Vector3(_robot.transform.position.x + boxSize.y / 2, _robot.transform.position.y + boxSize.x / 2, _robot.transform.position.z + boxSize.z / 2);
    //        StartCoroutine(Tumble(_tile, Vector3.up, pivotRobot, 90f));
    //        StartCoroutine(Tumble(_robot, Vector3.up, pivotRobot, 90f));
    //        yield return new WaitForSeconds(0.5f);
    //        pivotTile = new Vector3(_tile.transform.position.x + boxSize.x / 2, _tile.transform.position.y + boxSize.x / 2, _tile.transform.position.z - boxSize.y / 2);
    //        StartCoroutine(Tumble(_tile, Vector3.up, pivotTile, 90f));
    //        yield return new WaitForSeconds(1.0f);

    //        horizontalStep = horizontalStep + 2;
    //    }
    //    else if (stepBeam != 0 && stepBeam % 2 == 0)
    //    {
    //        //MoveHorizontal            
    //        pivotTile = new Vector3(_tile.transform.position.x - boxSize.y / 2, _tile.transform.position.y + boxSize.x / 2, _tile.transform.position.z - boxSize.z / 2);
    //        StartCoroutine(Tumble(_tile, Vector3.down, pivotTile, 180f));
    //        yield return new WaitForSeconds(0.5f);
    //        StartCoroutine(Tumble(_tile, Vector3.back, pivotTile, 180f));
    //        yield return new WaitForSeconds(0.5f);
    //        StartCoroutine(MovementHorizontalVertical(Vector3.up, Vector3.up, 90f, horizontalStep + 1, 0));
    //        yield return new WaitForSeconds((horizontalStep + 1) * 0.5f);
    //        //PlaceTile
    //        StartCoroutine(PlacingHorizontalVertical(180));
    //        yield return new WaitForSeconds(1.0f);


    //    }
    //    else if (stepBeam != 1 && stepBeam % 2 == 1)
    //    {
    //        //MoveHorizontal            
    //        pivotTile = new Vector3(_tile.transform.position.x - boxSize.y / 2, _tile.transform.position.y + boxSize.x / 2, _tile.transform.position.z - boxSize.z / 2);
    //        StartCoroutine(Tumble(_tile, Vector3.down, pivotTile, 180f));
    //        yield return new WaitForSeconds(0.5f);
    //        StartCoroutine(Tumble(_tile, Vector3.back, pivotTile, 180f));
    //        yield return new WaitForSeconds(0.5f);
    //        StartCoroutine(MovementHorizontalVertical(Vector3.up, Vector3.up, 90f, horizontalStep + 1, 0));
    //        yield return new WaitForSeconds((horizontalStep + 1) * 0.5f);
    //        //PlaceTile
    //        StartCoroutine(PlacingHorizontalVertical(90));
    //        yield return new WaitForSeconds(1.0f);
    //        horizontalStep = horizontalStep + 2;
    //    }
    //    stepBeam++;
    //}

}
