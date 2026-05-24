using UnityEngine;
using Units;       // Imports the name space for the enum types. 

public class techController : MonoBehaviour
{
    // Variables for unit health and damage scaling.
    private double[] playerUnitUpgrades = { 1.0, 1.0, 1.0 };
    private double[] enemyUnitUpgrades = { 1.0, 1.0, 1.0 };


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
                    if (GameManager.Instence.spendResources(owner, getCost(owner, type), 0, 0)) {
                        playerUnitUpgrades[0] += 0.1;
                        return;
                    }
                    else
                    {
                        Debug.Log("Not enough resources");
                        return;
                    }

                case 1:
                    if (GameManager.Instence.spendResources(owner, 0, getCost(owner, type), 0))
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
                    if (GameManager.Instence.spendResources(owner, 0, 0, getCost(owner, type)))
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

        GameManager.Instence
    }

    // Cost function for unit upgrades.
    public double getCost(UnitOwner owner, int type)
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
}
