using UnityEngine;

public static class ArrowDir
{
    public static Vector3 getArrowDirection(int direction){
        switch (direction){
            case 1:
                return new Vector3(90,0,0);
            case 2:
                return new Vector3(90,45,0);
            case 3:
                return new Vector3(90,135,0);
            case 4:
                return new Vector3(90,180,0);
            case 5:
                return new Vector3(90,225,0);
            case 6:
                return new Vector3(90,315,0);
            default:
                return new Vector3(0,0,0);
        }

    }

    public static Vector3 getPosition(int direction){
        switch (direction){
            case 1:
                return new Vector3(0f , .6f, 1.5f);
            case 2:
                return new Vector3(1f, .6f, .5f);
            case 3:
                return new Vector3(1f, .6f, -.5f);
            case 4:
                return new Vector3(0f , .6f, -1f);
            case 5:
                return new Vector3(-1f, .6f, -.5f);
            case 6:
                return new Vector3(-1f, .6f, .5f);
            default:
                return new Vector3(0,0,0);
        }
    }
}
