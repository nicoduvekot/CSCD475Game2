using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GlobalSound : MonoBehaviour
{
    
    private static AudioSource dynamicAudio;
    private static AudioClip soldierFight;
    private static AudioClip archerFight;
    private static AudioClip horsesFight;
    private static AudioClip unitDies;
    private static AudioClip horseDies;


    private static AudioClip horseMovement;
    private static AudioClip UnitMovement;
    private static AudioClip win;
    private static AudioClip lose;
    private static AudioClip capturePoint;
    private static AudioClip losePoint;

    private static Dictionary<string,float> coolDowns = new();

    void Start(){
        dynamicAudio = transform.GetComponent<AudioSource>();

        archerFight = Resources.Load("Sound/fireArrows", typeof(AudioClip)) as AudioClip;
        soldierFight = Resources.Load("Sound/swordFight", typeof(AudioClip)) as AudioClip;

        capturePoint = Resources.Load("Sound/capture", typeof(AudioClip)) as AudioClip;
        losePoint = Resources.Load("Sound/losePoint", typeof(AudioClip)) as AudioClip;
        
        

        coolDowns.Add("archerFight",5f);
        coolDowns.Add("swordFight",5f);

    }

    void Update(){
        foreach(string clip in coolDowns.Keys.ToList<string>()){
            if(coolDowns[clip] > 0){
                coolDowns[clip] -= Time.deltaTime;
            }
        }
    }

    public static void playMovement(){

    }

    public static void playFight(int unitType){
       
        if(unitType == 0 && coolDowns["swordFight"] <= 0f){
            dynamicAudio.PlayOneShot(soldierFight);
            coolDowns["swordFight"] = 7f;
        }else if(unitType == 1 && coolDowns["archerFight"] <= 0f){
            dynamicAudio.PlayOneShot(archerFight);
            coolDowns["archerFight"] = 5f;
        }else if(unitType == 2){

        }
        
    }

    public static void capturePOI(){
        dynamicAudio.PlayOneShot(capturePoint);
    }

    public static void losePOI(){
        dynamicAudio.PlayOneShot(losePoint);
    }

    


    


    
}
