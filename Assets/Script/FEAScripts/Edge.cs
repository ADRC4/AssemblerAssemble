 
public class Edge
{
    public Normal Direction;
    public Voxel LeftBottom;
    public Voxel RightBottom;
    public Voxel LeftTop;
    public Voxel RightTop;

    public Edge(Normal direction, Voxel leftBottom, Voxel rightBottom, Voxel leftTop, Voxel rightTop)
    {
        Direction = direction;
        LeftBottom = leftBottom;
        RightBottom = rightBottom;
        LeftTop = leftTop;
        RightTop = rightTop;
    }
}