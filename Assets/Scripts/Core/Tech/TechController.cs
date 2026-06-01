using UnityEngine;
using Units;       // Imports the name space for the enum types. 

public class TechController : MonoBehaviour
{
    // Variable for unit singleton
    public static TechController Instance { get; private set; }

    // Variables for unit health and damage scaling.
    private double[] playerUnitUpgrades = { 1.0, 1.0, 1.0 };
    private double[] enemyUnitUpgrades = { 1.0, 1.0, 1.0 };
    private double playerResourceUpgrade = 1.0;
    private double enemyResourceUpgrade = 1.0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Upgrade tech based off this. Code isn't the most pretty
    public void upgradeUnit(UnitOwner owner, int type)
    {
        if (type < 0 || type > 2)
        {
            Debug.Log("upgrade in techController must have an int between 0 and 2");
            return;
        }
        if (owner == UnitOwner.Player)
        {
            switch (type) {
                case 0:
                    if (GameManager.Instance.spendResources(owner, (int) getUnitCost(owner, type), 0, 0)) {
                        playerUnitUpgrades[0] += 0.1;
                        return;
                    }
                    else
                    {
                        Debug.Log("Not enough resources");
                        return;
                    }

                case 1:
                    if (GameManager.Instance.spendResources(owner, 0, (int) getUnitCost(owner, type), 0))
                    {
                        playerUnitUpgrades[1] += 0.1;
                        return;
                    }
                    else
                    {
                        Debug.Log("Not enough resources");
                        return;
                    }

                case 2:
                    if (GameManager.Instance.spendResources(owner, 0, 0, (int) getUnitCost(owner, type)))
                    {
                        playerUnitUpgrades[2] += 0.1;
                        return;
                    }
                    else
                    {
                        Debug.Log("Not enough resources");
                        return;
                    }

                default:
                    // Just in case
                    return;
            }
        }
        else if (owner == UnitOwner.Enemy)
        {
            return;
        }
        else
        {
            Debug.Log("UnitOwner types must be either Player or Enemy in getCost");
            return;
        }
    }

    // Cost function for unit upgrades.
    public double getUnitCost(UnitOwner owner, int type)
    {
        if(type < 0 || type > 2)
        {
            Debug.Log("getCost in techController must have an int between 0 and 2");
            return 0.0;
        }

        if (owner == UnitOwner.Player)
        {
            return 200 + (2000 * (playerUnitUpgrades[type] - 1));
        }
        else if (owner == UnitOwner.Enemy)
        {
            return 200 + (2000 * (enemyUnitUpgrades[type] - 1));
        }
        else
        {
            Debug.Log("UnitOwner types must be either Player or Enemy in getCost");
            return 0.0;
        }
    }

    // Used to reset the techs
    public void reset()
    {
        for (int i = 0; i < 3; i++) {
            playerUnitUpgrades[i] = 1.0;
            enemyUnitUpgrades[i] = 1.0;
        }
        playerResourceUpgrade = 1.0;
        enemyResourceUpgrade = 1.0;
    }

    public double getResourceCost(UnitOwner owner)
    {
        if (owner == UnitOwner.Player)
        {
            return 100 + (1000 * (playerResourceUpgrade - 1));
        }
        else if (owner == UnitOwner.Enemy)
        {
            return 100 + (1000 * (enemyResourceUpgrade - 1));
        }
        else
        {
            Debug.Log("UnitOwner types must be either Player or Enemy in getCost");
            return 0.0;
        }
    }

    public int getLevelUnit(UnitOwner owner, int type)
    {
        if(type < 0 || type > 2)
        {
            Debug.Log("getLevel in techController must have an int type between 0 and 2");
            return 0;
        }

        if (owner == UnitOwner.Player)
        {
            return (int) ((playerUnitUpgrades[type] - 1.0)/0.1);
        }
        else if (owner == UnitOwner.Enemy)
        {
            return (int) ((enemyUnitUpgrades[type] - 1.0) / 0.1);
        }
        else
        {
            Debug.Log("UnitOwner types must be either Player or Enemy in getLevelUnits");
            return 0;
        }
    }

    public void upgradeResource(UnitOwner owner)
    {
        int temp = (int)getResourceCost(owner);
        if (owner == UnitOwner.Player)
        {
            if (GameManager.Instance.spendResources(owner, temp, temp, temp))
            {
                playerResourceUpgrade += 0.2;
                return;
            }
        }
        else if (owner == UnitOwner.Enemy)
        {
            
            if (GameManager.Instance.spendResources(owner, temp, temp, temp))
            {
                enemyResourceUpgrade += 0.2;
                return;
            }
        }
        else
        {
            Debug.Log("UnitOwner types must be either Player or Enemy in getCost");
            return;
        }
    }

    public int getLevelResources(UnitOwner owner)
    {
        if(owner == UnitOwner.Player)
        {
            return (int) ((playerResourceUpgrade - 1) / 0.1);
        }
        else if(owner == UnitOwner.Enemy)
        {
            return (int) ((enemyResourceUpgrade - 1) / 0.1);
        }
        else
        {
            Debug.Log("UnitOwner types must be either Player or Enemy in getLevelResources");
            return 0;
        }
    }

    // Used to call resource generation for modifier
    public double getResourcesModifier(UnitOwner owner)
    {
        if (owner == UnitOwner.Player)
        {
            return playerResourceUpgrade;
        }
        else if (owner == UnitOwner.Enemy)
        {
            return enemyResourceUpgrade;
        }
        else
        {
            Debug.Log("UnitOwner types must be either Player or Enemy in getLevelResources");
            return 0;
        }
    }

    // Used to call units modifiers for tech
    public double getUnitsModifier(UnitOwner owner, int type)
    {
        if(type < 0 || type > 2)
        {
            Debug.Log("Type of the unit must be between 0 and 2");
            return 0;
        }

        if (owner == UnitOwner.Player)
        {
            return playerUnitUpgrades[type];
        }
        else if (owner == UnitOwner.Enemy)
        {
            return enemyUnitUpgrades[type];
        }
        else
        {
            Debug.Log("UnitOwner types must be either Player or Enemy in getLevelResources");
            return 0;
        }
    }
}
