//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class TileAnimationManager : MonoBehaviour
//{
//    public bool isBusy = false;
//    public float animDelayPerLayer = 0.0125f;

//    [SerializeField]
//    AnimationCurve tileUncoverCurve;
//    [SerializeField]
//    AnimationCurve areaUncoverCurve;
//    [SerializeField]
//    AnimationCurve tileFlagCurve;

//    GameManager gm;

//    void Start()
//    {
//        gm = GameManager.instance;
//    }

//    public void Animate(GameManager.Actions mode, float order)
//    {
//        isBusy = true;

//        if (mode == GameManager.Actions.AreaUncover)
//        {
//            int xPos = (int)transform.position.x;
//            int yPos = (int)transform.position.x;

//            uncover all blank spots
//            if (gm.nums[xPos, yPos] == 0)
//            {
//                for (int i = -1; i <= 1; i++)
//                {
//                    int sweepY = yPos + i;
//                    if (sweepY < 0 || sweepY > gm.height - 1) continue;

//                    for (int j = -1; j <= 1; j++)
//                    {
//                        int sweepX = xPos + j;
//                        if (sweepX < 0 || sweepX > gm.width - 1) continue;

//                        gm.tiles[sweepX, sweepY].animManager.Animate(GameManager.Actions.AreaUncover, order + 1);
//                    }
//                }
//            }
//        }

//        StartCoroutine(TileAnimation(mode, order + animDelayPerLayer));
//    }

//    IEnumerator TileAnimation(GameManager.Actions mode, float delay)
//    {
//        if (delay > 0) yield return new WaitForSeconds(delay);

//        AnimationCurve ac = new AnimationCurve();
//        bool disappear = true;
//        float totalTime = 0;

//        switch (mode)
//        {
//            case GameManager.Actions.Uncover:
//                ac = tileUncoverCurve;
//                totalTime = 0.4f;
//                break;
//            case GameManager.Actions.Flag:
//                ac = tileFlagCurve;
//                disappear = false;
//                totalTime = 0.25f;
//                break;
//            case GameManager.Actions.AreaUncover:
//                ac = areaUncoverCurve;
//                totalTime = 0.5f;
//                break;
//            default:
//                ac = tileUncoverCurve;
//                break;
//        }

//        float currentTime = 0;
//        while (currentTime <= totalTime)
//        {
//            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, ac.Evaluate(currentTime / totalTime));


//            currentTime += Time.deltaTime;
//            yield return null;
//        }

//        if (disappear) transform.gameObject.SetActive(false);
//    }
//}
