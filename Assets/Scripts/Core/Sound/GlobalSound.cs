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
    private static AudioClip recruitUnit;


    private static AudioClip horseMovement;
    private static AudioClip unitMovement;
    private static AudioClip win;
    private static AudioClip lose;
    private static AudioClip capturePoint;
    private static AudioClip losePoint;

    private static Dictionary<string,float> coolDowns = new();

    void Start(){
        dynamicAudio = transform.GetComponent<AudioSource>();

        unitMovement = Resources.Load("Sound/unitMove", typeof(AudioClip)) as AudioClip;
        horseMovement = Resources.Load("Sound/horseMove", typeof(AudioClip)) as AudioClip;

        archerFight = Resources.Load("Sound/fireArrows", typeof(AudioClip)) as AudioClip;
        soldierFight = Resources.Load("Sound/swordFight", typeof(AudioClip)) as AudioClip;
        horsesFight = Resources.Load("Sound/horseFight", typeof(AudioClip)) as AudioClip;

        capturePoint = Resources.Load("Sound/capture", typeof(AudioClip)) as AudioClip;
        losePoint = Resources.Load("Sound/losePoint", typeof(AudioClip)) as AudioClip;

        unitDies = Resources.Load("Sound/unitDies", typeof(AudioClip)) as AudioClip;
        horseDies = Resources.Load("Sound/horseDied", typeof(AudioClip)) as AudioClip;

        win = Resources.Load("Sound/win" , typeof(AudioClip)) as AudioClip;
        lose = Resources.Load("Sound/lose", typeof(AudioClip)) as AudioClip;    

        recruitUnit = Resources.Load("Sound/recruit" , typeof(AudioClip)) as AudioClip;    

        coolDowns.Add("archerFight",5f);
        coolDowns.Add("swordFight",5f);
        coolDowns.Add("horseFight",5f);

        coolDowns.Add("unitMovement",0f);
        coolDowns.Add("horseMovement",5f);

        coolDowns.Add("unitDies",1f);
        coolDowns.Add("horseDies",1f);
        coolDowns.Add("recruitUnit",1f);

    }

    void Update(){
        foreach(string clip in coolDowns.Keys.ToList<string>()){
            if(coolDowns[clip] > 0){
                coolDowns[clip] -= Time.deltaTime;
            }
        }
    }

    public static void playMovement(int unitType){
        if((unitType == 0 || unitType == 1) && coolDowns["unitMovement"] <= 0f){
                dynamicAudio.PlayOneShot(unitMovement,.2f);
                coolDowns["unitMovement"] = 5f;
        }else if(unitType == 2 && coolDowns["horseMovement"] <= 0f){
                dynamicAudio.PlayOneShot(horseMovement,.2f);
                coolDowns["horseMovement"] = 5f;
        }
    }

    public static void playFight(int unitType){
       
        if(unitType == 0 && coolDowns["swordFight"] <= 0f){
            dynamicAudio.PlayOneShot(soldierFight,.25f);
            coolDowns["swordFight"] = 7f;
        }else if(unitType == 1 && coolDowns["archerFight"] <= 0f){
            dynamicAudio.PlayOneShot(archerFight,.25f);
            coolDowns["archerFight"] = 5f;
        }else if(unitType == 2 && coolDowns["horsesFight"] <= 0f){
            dynamicAudio.PlayOneShot(horsesFight,.25f);
            coolDowns["horsesFight"] = 5f;
        }
        
    }

    public static void unitDead(int unitType){
        if((unitType == 0 || unitType  == 1) && coolDowns["unitDies"] <= 0f){
            dynamicAudio.PlayOneShot(unitDies,.5f);
            coolDowns["unitDies"] = 1f;
        }else if (unitType == 2 && coolDowns["horseFight"] <= 0f){
            dynamicAudio.PlayOneShot(horseDies,.2f);
            coolDowns["horseFight"] = 1f;
        }
        
    }

    public static void capturePOI(){
        dynamicAudio.PlayOneShot(capturePoint,.5f);
    }

    public static void losePOI(){
        dynamicAudio.PlayOneShot(losePoint);
    }

    public static void recruitUnitSound(){
        if(coolDowns["recruitUnit"] <= 0f){
            dynamicAudio.PlayOneShot(recruitUnit,.6f);
            coolDowns["recruitUnit"] = 1f;
        }
    }

    


    public static void winGame(){
        dynamicAudio.PlayOneShot(win);
    }

    public static void loseGame(){
        dynamicAudio.PlayOneShot(lose);
    }


    
}
