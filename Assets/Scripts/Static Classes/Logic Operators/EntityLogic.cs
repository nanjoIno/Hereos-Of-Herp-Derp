﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class EntityLogic
{
    // Get Living Entity Methods
    #region
    public static LivingEntity GetClosestEnemy(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetClosestEnemy() called for " + entity.name + "...");
        // TO DO: this method determines which defender is closest by drawing a straight line. It should instead calculate the closest by drawing a path to each with Astar

        LivingEntity closestTarget = null;
        float minimumDistance = Mathf.Infinity;

        // Declare new temp list for storing valid entities
        List<LivingEntity> potentialEnemies = new List<LivingEntity>();

        // Add all active entities that are NOT friendly to the temp list
        foreach (LivingEntity entityy in LivingEntityManager.Instance.allLivingEntities)
        {
            if(!CombatLogic.Instance.IsTargetFriendly(entity, entityy))
            {
                potentialEnemies.Add(entityy);
            }            
        }

        // Iterate throught the temp list to find the closest enemy to this character
        foreach (LivingEntity enemy in potentialEnemies)
        {
            float distancefromCharacter = Vector2.Distance(enemy.gameObject.transform.position, entity.transform.position);
            if (distancefromCharacter < minimumDistance)
            {
                closestTarget = enemy;
                minimumDistance = distancefromCharacter;
            }
        }
        return closestTarget;
    }
    public static LivingEntity GetClosestValidEnemy(LivingEntity entity)
    {
        // TO DO: this method determines which defender is closest by drawing a straight line. It should instead calculate the closest by drawing a path to each with Astar

        Debug.Log("EntityLogic.GetClosestValidEnemy() called for " + entity.name + "...");

        LivingEntity closestTarget = null;
        float minimumDistance = Mathf.Infinity;

        // Declare new temp list for storing valid entities
        List<LivingEntity> potentialEnemies = new List<LivingEntity>();

        // Add all active entities that are NOT friendly to the temp list
        foreach (LivingEntity entityy in LivingEntityManager.Instance.allLivingEntities)
        {
            if (!CombatLogic.Instance.IsTargetFriendly(entity, entityy))
            {
                potentialEnemies.Add(entityy);
            }

        }

        // Iterate throught the temp list to find the closest VALID enemy to this character
        foreach (LivingEntity enemy in potentialEnemies)
        {
            if (IsTargetVisible(entity, enemy))
            {
                float distancefromCharacter = Vector2.Distance(enemy.gameObject.transform.position, entity.transform.position);
                if (distancefromCharacter < minimumDistance)
                {
                    closestTarget = enemy;
                    minimumDistance = distancefromCharacter;
                }
            }
            
        }
        return closestTarget;
    }
    public static LivingEntity GetClosestAlly(LivingEntity entity, bool includeSelf = true)
    {
        Debug.Log("EntityLogic.GetClosestAlly() called for " + entity.name + "...");

        LivingEntity closestAlly = null;
        float minimumDistance = Mathf.Infinity;

        foreach (LivingEntity entityy in LivingEntityManager.Instance.allLivingEntities)
        {
            if(CombatLogic.Instance.IsTargetFriendly(entityy, entity))
            {
                float distancefromThisCharacter = Vector2.Distance(entityy.gameObject.transform.position, entity.transform.position);
                if (distancefromThisCharacter < minimumDistance && entityy != entity)
                {
                    if(
                        (includeSelf == true && entityy == entity) ||
                        (includeSelf == false && entityy != entity))
                    {
                        closestAlly = entityy;
                        minimumDistance = distancefromThisCharacter;
                    }
                    
                }
            }
            
        }


        if (closestAlly == null)
        {
            closestAlly = entity;
        }

        return closestAlly;
    }
    public static LivingEntity GetMostVulnerableEnemy(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetMostVulnerableEnemy() called for " + entity.name + "...");
        LivingEntity bestTarget = null;
        int pointScore = 0;

        foreach (LivingEntity entityy in LivingEntityManager.Instance.allLivingEntities)
        {
            if(!CombatLogic.Instance.IsTargetFriendly(entity, entityy))
            {
                int myPointScore = 1;

                if (entityy.myPassiveManager.vulnerable)
                {
                    myPointScore += 1;
                }

                if (myPointScore > pointScore)
                {
                    pointScore = myPointScore;
                    bestTarget = entityy;
                }
            }
            
        }

        return bestTarget;
    }
    public static LivingEntity GetEnemyWithLowestCurrentHP(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetEnemyWithLowestCurrentHP() called for " + entity.name + "...");
        LivingEntity bestTarget = null;
        int lowestHP = 1000;

        // Declare new temp list for storing defender 
        List<LivingEntity> potentialEnemies = new List<LivingEntity>();

        // Add all active defenders to the temp list
        foreach (LivingEntity entityy in LivingEntityManager.Instance.allLivingEntities)
        {
            if(!CombatLogic.Instance.IsTargetFriendly(entity, entityy))
            {
                potentialEnemies.Add(entityy);
            }            
        }

        foreach (LivingEntity entityy in potentialEnemies)
        {
            if (entityy.currentHealth < lowestHP)
            {
                bestTarget = entityy;
                lowestHP = entityy.currentHealth;
            }
        }

        if (bestTarget == null)
        {
            Debug.Log("GetDefenderWithLowestCurrentHP() returning null !!...");
        }

        return bestTarget;
    }
    public static List<LivingEntity> GetAllEnemiesWithinRange(LivingEntity entityFrom, int range)
    {
        Debug.Log("EntityLogic.GetAllEnemiesWithinRange() called for " + entityFrom.name + ", searching within a range of "+ range.ToString());
        List<Tile> tilesInRange = LevelManager.Instance.GetTilesWithinRange(range, entityFrom.tile);
        List<LivingEntity> enemiesInRange = new List<LivingEntity>();

        foreach (LivingEntity livingEntity in LivingEntityManager.Instance.allLivingEntities)
        {
            if (tilesInRange.Contains(livingEntity.tile) && 
                CombatLogic.Instance.IsTargetFriendly(entityFrom, livingEntity) == false)
            {
                enemiesInRange.Add(livingEntity);
            }
        }

        return enemiesInRange;
    }
    public static LivingEntity GetBestTarget(LivingEntity entity, bool prioritizeClosest = false, bool prioritizeLowHP = false, bool prioritizeMostVulnerable = false)
    {
        Debug.Log("EntityLogic.GetBestTarget() called for " + entity.name + "....");
        LivingEntity targetReturned = null;

        // Check for taunted first
        if (entity.myPassiveManager.taunted &&
            entity.myTaunter != null &&
            entity.myTaunter.inDeathProcess == false)
        {
            Debug.Log(entity.name + " is taunted by " + entity.myTaunter.name + ", returning this as best target...");
            targetReturned = entity.myTaunter;
        }
        else if (prioritizeClosest)
        {
            targetReturned = GetClosestValidEnemy(entity);
            if(targetReturned == null)
            {
                targetReturned = GetClosestEnemy(entity);
            }
        }
        else if (prioritizeLowHP)
        {
            targetReturned = GetEnemyWithLowestCurrentHP(entity);
        }
        else if (prioritizeMostVulnerable)
        {
            targetReturned = GetEnemyWithLowestCurrentHP(entity);
        }
        else
        {
            targetReturned = null;
        }

        // Check for null
        if(targetReturned == null)
        {
            Debug.Log("EntityLogic.GetBestTarget() returned a null value...");
        }
        else if (targetReturned != null)
        {
            Debug.Log("EntityLogic.GetBestTarget() returned " + targetReturned.myName + " as the best target for " + entity.name);
        }
           
        return targetReturned;
    }    

    #endregion

    // Conditional Checks + Booleans
    #region
    public static bool IsAbleToMove(LivingEntity entity)
    {
        Debug.Log("EntityLogic.IsAbleToMove() called for " + entity.name + "....");

        if (entity.myPassiveManager.immobilized)
        {
            Debug.Log(entity.name + " is not able to move due to being immobilized....");
            if (entity.defender)
            {
                InvalidActionManager.Instance.ShowNewErrorMessage("This character is Immobilized");
            }
            return false;
        }
        else if(GetTotalMobility(entity) <= 0)
        {
            Debug.Log(entity.name + " is not able to move due to having 0 mobility....");
            if (entity.defender)
            {
                InvalidActionManager.Instance.ShowNewErrorMessage("This character has 0 Mobility");
            }
            return false;
        }
        else
        {
            Debug.Log(entity.name + " is able to move...");
            return true;
        }

    }
    public static bool HasEnoughEnergy(LivingEntity entity, Ability ability)
    {
        Debug.Log("EntityLogic.HasEnoughAP() called for " + entity.name + " with ability " + ability.abilityName);

        if (entity.currentEnergy >= ability.abilityEnergyCost)
        {
            Debug.Log(entity.name + " has enough energy to use " + ability.abilityName);
            return true;
        }

        else if (entity.myPassiveManager.preparation)
        {
            Debug.Log(entity.name + " has enough energy to use " + ability.abilityName + " because of 'Preparation' passive");
            return true;
        }

        else if (entity.myPassiveManager.flux &&
                entity.moveActionsTakenThisActivation == 0 &&
                ability.abilityName == "Move")
        {
            Debug.Log(entity.name + " has enough energy to use " + ability.abilityName + " because of 'Flux' passive");
            return true;
        }

        else
        {
            Debug.Log(entity.name + " does not meet the energy requirments of " + ability.abilityName);
            return false;
        }
    }
    public static bool IsAbleToTakeActions(LivingEntity entity)
    {
        Debug.Log("EntityLogic.IsAbleToTakeActions() called for " + entity.name + "...");

        if (entity.myPassiveManager.stunned)
        {
            Debug.Log(entity.name + " is unable to take actions. REASON: 'Stunned' passive");
            if (entity.defender)
            {
                InvalidActionManager.Instance.ShowNewErrorMessage("This character is Stunned");
            }
            return false;
        }
        else if (entity.myPassiveManager.sleep)
        {
            Debug.Log(entity.name + " is unable to take actions. REASON: 'Sleep' passive");
            if (entity.defender)
            {
                InvalidActionManager.Instance.ShowNewErrorMessage("This character is Sleeping");
            }
            return false;
        }
        else
        {
            Debug.Log(entity.name + " is able to take actions");
            return true;
        }
    }
    public static bool IsTargetInRange(LivingEntity caster, LivingEntity target, int range, bool ignoreLoS = false)
    {
        Debug.Log("EntityLogic.IsTargetInRange() called for " + caster.myName + ", ignore LoS = " + ignoreLoS.ToString());

        List<Tile> tilesWithinMyRange = LevelManager.Instance.GetTilesWithinRange(range, caster.tile, false, ignoreLoS);

        if (target == null)
        {
            Debug.Log("IsTargetInRange() target value is null...");
            return false;
        }

        Tile targetsTile = target.tile;

        if (!IsTargetVisible(caster, target))
        {
            Debug.Log("Target is not visible");
            return false;
        }

        else if (!tilesWithinMyRange.Contains(targetsTile))
        {
            Debug.Log("Target is out of range");
            if (caster.defender)
            {
                InvalidActionManager.Instance.ShowNewErrorMessage("Target is out of range");
            }
            return false;
        }      

        else if (tilesWithinMyRange.Contains(targetsTile) && IsTargetVisible(caster, target))
        {
            Debug.Log("Target enemy is in range");
            return true;
        }

        else
        {
            Debug.Log("Target enemy is NOT range");
            if (caster.defender)
            {
                InvalidActionManager.Instance.ShowNewErrorMessage("Target is out of range");
            }
            return false;
        }
    }
    public static bool IsTargetVisible(LivingEntity caster, LivingEntity target)
    {
        Debug.Log("EntityLogic.IsTargetVisible() called for " + caster.myName + " against target " + target.myName);

        List<Tile> tilesWithinStealthSight = LevelManager.Instance.GetTilesWithinRange(1, caster.tile);
        bool passedStealthCheck = false;
        bool passedLosCheck = false;

        // check for null target
        if(target == null)
        {
            Debug.Log("IsTargetVisible() target recieved is null, returning false...");
            return false;
        }

        // check for stealth
        if (tilesWithinStealthSight.Contains(target.tile) == false &&
            (target.myPassiveManager.camoflage || target.myPassiveManager.stealth) &&
            (CombatLogic.Instance.IsTargetFriendly(caster, target) == false) &&
             (caster.myPassiveManager.trueSight == false && caster.myPassiveManager.temporaryTrueSight == false)
            )
        {
            Debug.Log("IsTargetVisible() determined that " + target.myName + " CANNOT be seen by "+ caster.myName + "...");
            if (caster.defender)
            {
                InvalidActionManager.Instance.ShowNewErrorMessage("Target is Camoflaged");
            }
            passedStealthCheck = false;
        }

        else
        {
            Debug.Log("IsTargetVisible() determined that " + target.myName + " CAN be seen by " + caster.myName + "...");
            passedStealthCheck = true;
        }

        // check for LoS
        if(PositionLogic.Instance.IsThereLosFromAtoB(caster.tile, target.tile))
        {
            Debug.Log("IsTargetVisible() determined that " + caster.myName + " has line of sight to " + target.myName + "...");
            passedLosCheck = true;
        }
        // check if caster has ignores LoS to target
        else if (PositionLogic.Instance.IsThereLosFromAtoB(caster.tile, target.tile) == false &&
                 caster.myPassiveManager.etherealBeing)
        {
            Debug.Log("IsTargetVisible() determined that " + caster.myName + " does not have direct line of sight to " + target.myName + ", but has 'Ethereal Being' passive...");
            passedLosCheck = true;
        }
        else
        {
            Debug.Log("IsTargetVisible() determined that " + caster.myName + " DOES NOT have line of sight to " + target.myName + "...");
            if (caster.defender)
            {
                InvalidActionManager.Instance.ShowNewErrorMessage("No line of sight to the target");
            }
            passedLosCheck = false;
        }

        // return the result of the checks
        if(passedLosCheck == true && passedStealthCheck == true)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    private static bool IsAbilityOffCooldown(Ability ability)
    {
        Debug.Log("EntityLogic.IsAbilityOffCooldown() called for " + ability.abilityName + "...");
        if (ability.abilityCurrentCooldownTime == 0)
        {
            return true;
        }
        else
        {
            Debug.Log("Cannot use ability: Ability is on cooldown");
            return false;
        }
    }


    // IsAbilityUseable should also check against the target for things like taunt
    public static bool IsAbilityUseable(LivingEntity caster, Ability ability, LivingEntity target = null)
    {
        if(ability == null)
        {
            Debug.Log("WARNING!: EntityLogic.IsAbilityUseable() was given a NULL ability from" + caster.myName); ;
        }

        Debug.Log("EntityLogic.IsAbilityUseable() called for " + caster.myName + " using ability " + ability.abilityName);

        // Check energy
        if(!HasEnoughEnergy(caster, ability))
        {
            Debug.Log(ability.abilityName + " use is invalid. REASON: " + caster.myName + " does not have enough Energy");
            if (caster.defender)
            {
                InvalidActionManager.Instance.ShowNewErrorMessage("Not enough Energy");
            }
           
            return false;
        }

        // Check cooldown
        if (!IsAbilityOffCooldown(ability))
        {
            Debug.Log(ability.abilityName + " use is invalid. REASON: " + ability.abilityName + " is on cooldown");
            if (caster.defender)
            {
                InvalidActionManager.Instance.ShowNewErrorMessage("Ability is on cooldown");
            }
            return false;
        }

        // Check weapon requirments met
        if (!AbilityLogic.Instance.DoesAbilityMeetWeaponRequirements(caster, ability))
        {
            Debug.Log(ability.abilityName + " use is invalid. REASON: " + caster.myName + " does not meet weapon requirments" +
                "of ability");
            if (caster.defender)
            {
                InvalidActionManager.Instance.ShowNewErrorMessage("Weapon requirments of ability not met");
            }
            return false;
        }

        // Check disarmed
        if (ability.abilityType == AbilityDataSO.AbilityType.MeleeAttack && caster.myPassiveManager.disarmed)
        {
            Debug.Log(ability.abilityName + " use is invalid. REASON: " + caster.myName + " is disarmed");
            if (caster.defender)
            {
                InvalidActionManager.Instance.ShowNewErrorMessage("This character is Disarmed");
            }
            return false;
        }

        // Check blind
        if (ability.abilityType == AbilityDataSO.AbilityType.RangedAttack && caster.myPassiveManager.blind)
        {
            Debug.Log(ability.abilityName + " use is invalid. REASON: " + caster.myName + " is blind");
            if (caster.defender)
            {
                InvalidActionManager.Instance.ShowNewErrorMessage("This character is Blind");
            }
            return false;
        }

        // Check silenced
        if (ability.abilityType == AbilityDataSO.AbilityType.Skill && caster.myPassiveManager.silenced)
        {
            Debug.Log(ability.abilityName + " use is invalid. REASON: " + caster.myName + " is silenced");
            if (caster.defender)
            {
                InvalidActionManager.Instance.ShowNewErrorMessage("This character is Silenced");
            }
            return false;
        }

        // Check taunted
        if (target != null &&
            caster.myPassiveManager.taunted &&
            caster.myTaunter != target)
        {
            Debug.Log(ability.abilityName + " use is invalid. REASON: " + caster.myName + " is taunted, and " +
               target.myName + " is not it's taunter");
            if (caster.defender)
            {
                InvalidActionManager.Instance.ShowNewErrorMessage("This character is Taunted");
            }
            return false;
        }

        // Resolve
        Debug.Log(ability.abilityName + " use by " + caster.myName + " is valid");
        return true;

    }
    public static bool CanPerformAbilityTwoAfterAbilityOne(Ability abilityOne, Ability abilityTwo, LivingEntity entity)
    {
        Debug.Log("EntityLogic.CanPerformAbilityTwoAfterAbilityOne() called for " + entity.name 
            + " using ability " + abilityOne.abilityName + " before ability " + abilityTwo.abilityName);

        // method is mainly used to stop enemies from move forward too agressively.
        // prevents enemies from move into a defenders range without being able to attack.
        // some enemies should only advance if they can make attacks after moving (like squishy melee fighters, or ranged attackers)

        int currentAP = entity.currentEnergy;

        if(currentAP - abilityOne.abilityEnergyCost >= abilityTwo.abilityEnergyCost)
        {
            Debug.Log("CanPerformAbilityTwoAfterAbilityOne() calculated that " + entity.name + " has enough AP to perform " + 
                abilityTwo.abilityName + " after " + abilityOne.abilityName);
            return true;
        }
        else
        {
            Debug.Log("CanPerformAbilityTwoAfterAbilityOne() calculated that " + entity.name + " DOES NOT have enough AP to perform " +
                abilityTwo.abilityName + " after " + abilityOne.abilityName);
            return false;
        }
    }
    public static bool IsTargetOfAbilityValid(LivingEntity caster, LivingEntity target, Ability abilityUsed)
    {
        // method checks the validity of an ability being used against a certain target
        // used for checking if a character is taunted by another

        return true;
    }
    #endregion

    // Get Tiles + Pathfinding Logic
    #region
    public static Tile GetValidGrassTileWithinRange(LivingEntity entityFrom, int range)
    {
        Debug.Log("EntityLogic.GetValidGrassTileWithinRange() called for " + entityFrom.name + " within a range of " + range.ToString());
        Tile closestGrassTile = null;
        List<Tile> adjacentTiles = LevelManager.Instance.GetValidMoveableTilesWithinRange(range, entityFrom.tile);

        foreach (Tile tile in adjacentTiles)
        {
            if (tile.myTileType == Tile.TileType.Grass && tile.IsEmpty && tile.IsWalkable)
            {
                closestGrassTile = tile;
            }
        }

        if(closestGrassTile == null)
        {
            Debug.Log("EntityLogic.GetValidGrassTileWithinRange() could not find a valid grass tile within " +
                range.ToString() + ", returning a null tile...");
        }

        else
        {
            Debug.Log("EntityLogic.GetValidGrassTileWithinRange() returning tile " + closestGrassTile.GridPosition.X.ToString() +
                ", " + closestGrassTile.GridPosition.Y.ToString());
        }
        return closestGrassTile;
    }
    public static Tile GetFurthestTileFromTargetWithinRange(LivingEntity originCharacter, LivingEntity target, int range)
    {
        Debug.Log("EntityLogic.GetFurthestTileFromTargetWithinRange() called for " + 
            originCharacter.name + ", with a range of " + range.ToString() + " from " + target.name);

        // method used to enemy AI move away from a defender logically to safety
        List<Tile> tilesWithinRangeOfOriginCharacter = LevelManager.Instance.GetValidMoveableTilesWithinRange(range, originCharacter.tile);
        return LevelManager.Instance.GetFurthestTileFromTargetFromList(tilesWithinRangeOfOriginCharacter, target.tile);
    }
    public static Tile GetBestValidMoveLocationBetweenMeAndTarget(LivingEntity characterActing, LivingEntity target, int rangeFromTarget, int movePoints)
    {
        Debug.Log("EntityLogic.GetBestValidMoveLocationBetweenMeAndTarget() called for " +
           characterActing.name + ", with a range of " + rangeFromTarget.ToString() + " from " + target.name + " using " + movePoints.ToString() + " move points");

        Tile tileReturned = null;
        Tile tileClosestToTarget = LevelManager.Instance.GetClosestValidTile(LevelManager.Instance.GetTilesWithinRange(rangeFromTarget, target.tile), characterActing.tile);

        Stack<Node> pathFromMeToIdealTile = MovementLogic.Instance.GeneratePath(characterActing.tile.GridPosition, tileClosestToTarget.GridPosition);

        Debug.Log("GetBestValidMoveLocationBetweenMeAndTarget() generated a path with " + pathFromMeToIdealTile.Count.ToString() + " tiles on it");        

        
        if (pathFromMeToIdealTile.Count > 1 && pathFromMeToIdealTile.Count < movePoints)
        {
            tileReturned = LevelManager.Instance.GetTileFromPointReference(pathFromMeToIdealTile.ElementAt(pathFromMeToIdealTile.Count - 1).GridPosition);
        }       

        else if (pathFromMeToIdealTile.Count > 1)
        {
            tileReturned = LevelManager.Instance.GetTileFromPointReference(pathFromMeToIdealTile.ElementAt(movePoints - 1).GridPosition);
        }

        else if (pathFromMeToIdealTile.Count == 1)
        {
            tileReturned = LevelManager.Instance.GetTileFromPointReference(pathFromMeToIdealTile.ElementAt(0).GridPosition);
        }

        if (tileReturned == null)
        {
            Debug.Log("GetBestValidMoveLocationBetweenMeAndTarget() could not draw a valid path from" + characterActing.name +
                " to " + target.name + ", returning a null Tile destination...");
        }
        else if (tileReturned != null)
        {
            Debug.Log("GetBestValidMoveLocationBetweenMeAndTarget() determined that the best move location between " +
                characterActing.name + " and " + target.name + " is Tile " + tileReturned.GridPosition.X.ToString() + ", " + tileReturned.GridPosition.Y.ToString());
        }

        return tileReturned;

    }

    #endregion

    // Get Stats And Attribute Total Values
    #region
    // Core stats
    public static int GetTotalDexterity(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetTotalDexterity() called for " + entity.name + "...");
        // Get base dexterity
        int dexterityReturned = entity.currentDexterity;
        Debug.Log(entity.name + " base dexterity: " + dexterityReturned.ToString());

        // Add from bonus dexterity passive
        if (entity.myPassiveManager.bonusDexterity)
        {
            dexterityReturned += entity.myPassiveManager.bonusDexterityStacks;
            Debug.Log("Value after bonus dexterity added: " + dexterityReturned.ToString());
        }

        // Add from temporary bonus dexterity passive
        if (entity.myPassiveManager.temporaryBonusDexterity)
        {
            dexterityReturned += entity.myPassiveManager.temporaryBonusDexterityStacks;
            Debug.Log("Value after temporary bonus dexterity added: " + dexterityReturned.ToString());
        }

        // Add bonus from purity passive
        if (entity.myPassiveManager.purity)
        {
            dexterityReturned += 2;
            Debug.Log("Value after purity bonus added: " + dexterityReturned.ToString());
        }

        // return final value
        Debug.Log("Final dexterity value calculated: " + dexterityReturned.ToString());
        return dexterityReturned;
    }
    public static int GetTotalStrength(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetTotalStrength() called for " + entity.name + "...");
        // Get base strength
        int strengthReturned = entity.currentStrength;
        Debug.Log(entity.name + " base strength: " + strengthReturned.ToString());

        // Add bonus strength
        if (entity.myPassiveManager.bonusStrength)
        {
            strengthReturned += entity.myPassiveManager.bonusStrengthStacks;
            Debug.Log("Value after bonus strength added: " + strengthReturned.ToString());
        }

        // Add temporary bonus strength
        if (entity.myPassiveManager.temporaryBonusStrength)
        {
            strengthReturned += entity.myPassiveManager.temporaryBonusStrengthStacks;
            Debug.Log("Value after temporary bonus strength added: " + strengthReturned.ToString());
        }            

        // Add bonus from purity passive
        if (entity.myPassiveManager.purity)
        {
            strengthReturned += 2;
            Debug.Log("Value after purity bonus added: " + strengthReturned.ToString());
        }

        // return final value
        Debug.Log("Final strength value calculated: " + strengthReturned.ToString());
        return strengthReturned;
    }
    public static int GetTotalWisdom(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetTotalWisdom() called for " + entity.name + "...");

        // Get base wisdom
        int wisdomReturned = entity.currentWisdom;
        Debug.Log(entity.name + " base wisdom: " + wisdomReturned.ToString());

        // Add bonus wisdom
        if (entity.myPassiveManager.bonusWisdom)
        {
            wisdomReturned += entity.myPassiveManager.bonusWisdomStacks;
            Debug.Log("Value after bonus wisdom added: " + wisdomReturned.ToString());
        }

        // Add temporary bonus wisdom
        if (entity.myPassiveManager.temporaryBonusWisdom)
        {
            wisdomReturned += entity.myPassiveManager.temporaryBonusWisdomStacks;
            Debug.Log("Value after temporary bonus wisdom added: " + wisdomReturned.ToString());
        }            

        // Add bonus from purity passive
        if (entity.myPassiveManager.purity)
        {
            wisdomReturned += 2;
            Debug.Log("Value after purity bonus added: " + wisdomReturned.ToString());
        }

        // return final value
        Debug.Log("Final wisdom value calculated: " + wisdomReturned.ToString());
        return wisdomReturned;
    }
    public static int GetTotalInitiative(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetTotalInitiative() called for " + entity.name + "...");

        // Get base Initiative
        int initiativeReturned = entity.currentInitiative;
        Debug.Log(entity.name + " base initiative: " + initiativeReturned.ToString());

        // Add bonus Initiative
        if (entity.myPassiveManager.bonusInitiative)
        {
            initiativeReturned += entity.myPassiveManager.bonusInitiativeStacks;
            Debug.Log("Value after bonus initiative added: " + initiativeReturned.ToString());
        }        

        // Add temporary bonus Initiative
        if (entity.myPassiveManager.temporaryBonusInitiative)
        {
            initiativeReturned += entity.myPassiveManager.temporaryBonusInitiativeStacks;
            Debug.Log("Value after temporary bonus initiative added: " + initiativeReturned.ToString());
        }        

        // Reduce by 1 if 'Chilled'
        if (entity.myPassiveManager.chilled)
        {
            initiativeReturned -= 1;
            Debug.Log("Value after bonus 'Chilled' reduction: " + initiativeReturned.ToString());
        }

        // Prevent going below zero
        if (initiativeReturned < 0)
        {
            initiativeReturned = 0;
        }

        // return final value
        Debug.Log("Final initiative value calculated: " + initiativeReturned.ToString());
        return initiativeReturned;
    }
    public static int GetTotalMobility(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetTotalMobility() called for " + entity.myName + "...");

        // Get base Mobility
        int mobilityReturned = entity.currentMobility;
        Debug.Log(entity.myName + " base mobility: " + mobilityReturned.ToString());

        // Add bonus Mobility
        if (entity.myPassiveManager.bonusMobility)
        {
            mobilityReturned += entity.myPassiveManager.bonusMobilityStacks;
            Debug.Log("Value after bonus mobility added: " + mobilityReturned.ToString());
        }


        // Add temporary bonus Mobility
        if (entity.myPassiveManager.temporaryBonusMobility)
        {
            mobilityReturned += entity.myPassiveManager.temporaryBonusMobilityStacks;
            Debug.Log("Value after temporary bonus mobility added: " + mobilityReturned.ToString());
        }
        

        // Reduce by 1 if 'Chilled'
        if (entity.myPassiveManager.chilled)
        {
            mobilityReturned -= 1;
            Debug.Log("Value after 'Chilled' reduction: " + mobilityReturned.ToString());
        }

        // Check for Patient Stalker
        if (entity.myPassiveManager.patientStalker &&
          (entity.myPassiveManager.camoflage || entity.myPassiveManager.stealth)
          )
        {
            mobilityReturned += 1;
            Debug.Log("Value after 'Patient Stalker' bonus: " + mobilityReturned.ToString());
        }

        // Prevent going below zero
        if (mobilityReturned < 0)
        {
            mobilityReturned = 0;
        }

        // return final value
        Debug.Log("Final mobility value calculated: " + mobilityReturned.ToString());
        return mobilityReturned;
    }
    public static int GetTotalStamina(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetTotalStamina() called for " + entity.myName + "...");

        // Get base Stamina
        int staminaReturned = entity.currentStamina;
        Debug.Log(entity.name + " base stamina: " + staminaReturned.ToString());

        // Add bonus Stamina
        if (entity.myPassiveManager.bonusStamina)
        {
            staminaReturned += entity.myPassiveManager.bonusStaminaStacks;
            Debug.Log("Value after bonus stamina added: " + staminaReturned.ToString());
        }

        // Add temporary bonus Stamina
        if (entity.myPassiveManager.temporaryBonusStamina)
        {
            staminaReturned += entity.myPassiveManager.temporaryBonusStaminaStacks;
            Debug.Log("Value after temporary bonus stamina added: " + staminaReturned.ToString());
        }
       

        // Check for Patient Stalker
        if(entity.myPassiveManager.patientStalker && 
          (entity.myPassiveManager.camoflage || entity.myPassiveManager.stealth)
          )
        {
            staminaReturned += 20;
            Debug.Log("Value after 'Patient Stalker' bonus added: " + staminaReturned.ToString());
        }

        // Minus 10 if 'Shocked'
        if (entity.myPassiveManager.shocked)
        {
            staminaReturned -= 10;
            Debug.Log("Value after 'Shocked' reduction: " + staminaReturned.ToString());
        }

        // prevent reducing below 0
        if(staminaReturned < 0)
        {
            staminaReturned = 0;
        }

        // return final value
        Debug.Log("Final stamina value calculated: " + staminaReturned.ToString());
        return staminaReturned;
    }

    // Secondary Stats
    public static int GetTotalRangeOfRangedAttack(LivingEntity entity, Ability ability)
    {
        Debug.Log("EntityLogic.GetTotalRangeOfRangedAttack() called for " + entity.name + "...");
        int rangeReturned = 0;

        // Get base range from ability
        rangeReturned = ability.abilityRange;
        Debug.Log(entity.name + " base ability range: " + rangeReturned.ToString());

        // Get range bonus from hawk eye passive
        if (entity.myPassiveManager.hawkEye)
        {
            rangeReturned += entity.myPassiveManager.hawkEyeStacks;
            Debug.Log("Value after 'Hawk Eye' bonus added: " + rangeReturned.ToString());
        }

        // Get range bonus from TEMPORARY hawk eye passive
        if (entity.myPassiveManager.temporaryHawkEye)
        {
            rangeReturned += entity.myPassiveManager.temporaryHawkEyeStacks;
            Debug.Log("Value after 'Temporary Hawk Eye' bonus added: " + rangeReturned.ToString());
        }

        // return final value
        Debug.Log("Final ranged attack range value calculated: " + rangeReturned.ToString());
        return rangeReturned;
    }
    public static int GetTotalDodge(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetTotalDodge() called for " + entity.name + "...");

        // Get base Dodge
        int dodgeReturned = entity.currentDodgeChance;
        Debug.Log(entity.name + " base dodge: " + dodgeReturned.ToString());

        // Add temporary bonus Dodge
        if (entity.myPassiveManager.temporaryBonusDodge)
        {
            dodgeReturned += entity.myPassiveManager.temporaryBonusDodgeStacks;
            Debug.Log("Value after temporary bonus dodge added: " + dodgeReturned.ToString());
        }        

        // Add nimble
        if (entity.myPassiveManager.nimble)
        {
            dodgeReturned += 10;
            Debug.Log("Value after 'Nimble' passive bonus: " + dodgeReturned.ToString());
        }

        // Add perfect reflexes
        if (entity.myPassiveManager.perfectReflexes)
        {
            dodgeReturned += 30;
            Debug.Log("Value after 'Perfect Reflexes' passive bonus: " + dodgeReturned.ToString());
        }

        // return final value
        Debug.Log("Final dodge value calculated: " + dodgeReturned.ToString());
        return dodgeReturned;
    }
    public static int GetTotalParry(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetTotalParry() called for " + entity.name + "...");

        // Get base Parry
        int parryReturned = entity.currentParryChance;
        Debug.Log(entity.name + " base parry: " + parryReturned.ToString());

        // Add temporary bonus Parry
        if (entity.myPassiveManager.temporaryBonusParry)
        {
            parryReturned += entity.myPassiveManager.temporaryBonusParryStacks;
            Debug.Log("Value after temporary bonus parry added: " + parryReturned.ToString());
        }     

        // Add nimble
        if (entity.myPassiveManager.nimble)
        {
            parryReturned += 10;
            Debug.Log("Value after 'Nimble' passive bonus: " + parryReturned.ToString());
        }

        // Add perfect reflexes
        if (entity.myPassiveManager.perfectReflexes)
        {
            parryReturned += 30;
            Debug.Log("Value after 'Perfect Reflexes' passive bonus: " + parryReturned.ToString());
        }

        // return final value
        Debug.Log("Final parry value calculated: " + parryReturned.ToString());
        return parryReturned;
    }
    public static int GetTotalCriticalChance(LivingEntity entity, Ability ability = null)
    {
        Debug.Log("EntityLogic.GetTotalCritical() called for " + entity.name + "...");

        // Get base Crit
        int criticalReturned = entity.currentCriticalChance;
        Debug.Log(entity.name + " base crit: " + criticalReturned.ToString());

        // Consider ability first
        if (ability != null)
        {
            if(ability.abilityType == AbilityDataSO.AbilityType.MeleeAttack)
            {
                // TO DO!: Add melee specific crit modifiers here
                criticalReturned += GetTotalMeleeCriticalChance(entity);
            }
            else if (ability.abilityType == AbilityDataSO.AbilityType.MeleeAttack)
            {
                // TO DO!: Add ranged specific crit modifiers here
                criticalReturned += GetTotalRangedCriticalChance(entity);
            }

            // Check for Predator
            if (entity.myPassiveManager.predator)
            {
               if((ability.abilityType == AbilityDataSO.AbilityType.MeleeAttack ||
               ability.abilityType == AbilityDataSO.AbilityType.RangedAttack) &&
               (entity.myPassiveManager.camoflage || entity.myPassiveManager.stealth)
               )
               {
                    criticalReturned += 50;
                    Debug.Log("Value after 'Predator' passive bonus: " + criticalReturned.ToString());
                }
            }           

        }

        // Check for Masochist
        if (entity.myPassiveManager.masochist &&
           ((entity.currentMaxHealth / 2) > entity.currentHealth)
           )
        {
            criticalReturned += 50;
            Debug.Log("Value after 'Masochist' passive bonus: " + criticalReturned.ToString());
        }

        // Check for Berserk
        if (entity.myPassiveManager.berserk)
        {
            criticalReturned += 100;
            Debug.Log("Value after 'Berserk' passive bonus: " + criticalReturned.ToString());
        }

        // Dont go over 100
        if(criticalReturned > 100)
        {
            criticalReturned = 100;
        }

        // return final value
        Debug.Log("Final crit value calculated: " + criticalReturned.ToString());
        return criticalReturned;
    }
    public static int GetTotalMeleeCriticalChance(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetTotalMeleeCritical() called for " + entity.name + "...");

        // Get base melee Crit
        int criticalReturned = 0;

        // Check recklessness
        if (entity.myPassiveManager.recklessness)
        {
            criticalReturned += 20;
            Debug.Log("Value after 'Recklessness' passive bonus: " + criticalReturned.ToString());
        }

        // return final value
        return criticalReturned;
    }
    public static int GetTotalRangedCriticalChance(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetTotalRangedCritical() called for " + entity.name + "...");

        // Get base ranged Crit
        int criticalReturned = 0;

        // Check for 'Concentration' passive
        if (entity.myPassiveManager.concentration)
        {
            criticalReturned += 20;
            Debug.Log("Value after 'Concentration' passive bonus: " + criticalReturned.ToString());
        }

        // return final value
        return criticalReturned;
    }
    public static int GetTotalAuraSize(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetTotalAuraSize() called for " + entity.name + "...");

        // Get base ranged Crit
        int auraSizeReturned = entity.currentAuraSize;

        // Check for 'Radiance' passive
        if (entity.myPassiveManager.radiance)
        {
            auraSizeReturned += entity.myPassiveManager.radianceStacks;
            Debug.Log("Value after 'Radiance' passive bonus: " + auraSizeReturned.ToString());
        }

        // return final value
        return auraSizeReturned;
    }

    // Resistances
    public static int GetTotalResistance(LivingEntity entity, string resistanceType)
    {
        Debug.Log("EntityLogic.GetTotalResistance() called for " + entity.name + " checking " + resistanceType + " resistance...");

        int targetResistance = 0;

        // get the targets resistance value
        if (resistanceType == "Physical")
        {
            targetResistance = GetTotalPhysicalResistance(entity);
        }
        else if (resistanceType == "Fire")
        {
            targetResistance = GetTotalFireResistance(entity);
        }
        else if (resistanceType == "Air")
        {
            targetResistance = GetTotalAirResistance(entity);
        }
        else if (resistanceType == "Poison")
        {
            targetResistance = GetTotalPoisonResistance(entity);
        }
        else if (resistanceType == "Shadow")
        {
            targetResistance = GetTotalShadowResistance(entity);
        }
        else if (resistanceType == "Frost")
        {
            targetResistance = GetTotalFrostResistance(entity);
        }

        // Check Infuse passive
        if (entity.myPassiveManager.infuse && 
            (resistanceType == "Physical" ||
             resistanceType == "Fire" ||
             resistanceType == "Frost" ||
             resistanceType == "Poison" ||
             resistanceType == "Shadow" ||
             resistanceType == "Air"))
        {
            targetResistance += 20;
            Debug.Log("Value after 'Infuse' passive bonus: " + targetResistance.ToString());
        }

        // Finish
        Debug.Log("Final resistance value calculated: " + targetResistance.ToString());
        return targetResistance;
    }
    private static int GetTotalPhysicalResistance(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetTotalPhysicalResistance() called for " + entity.name + "...");

        // Get base resistance
        int physicalResistanceReturned = entity.currentPhysicalResistance;
        Debug.Log(entity.name + " base physical resistance: " + physicalResistanceReturned.ToString());

        // return final value
        return physicalResistanceReturned;
    }
    private static int GetTotalFireResistance(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetTotalFireResistance() called for " + entity.name + "...");

        // Get base resistance
        int fireResistanceReturned = entity.currentFireResistance;
        Debug.Log(entity.name + " base fire resistance: " + fireResistanceReturned.ToString());

        // Check Demon passive
        if (entity.myPassiveManager.demon)
        {
            fireResistanceReturned += 30;
            Debug.Log("Value after 'Demon' passive bonus: " + fireResistanceReturned.ToString());
        }

        // return final value
        return fireResistanceReturned;
    }
    private static int GetTotalShadowResistance(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetTotalShadowResistance() called for " + entity.name + "...");

        // Get base resistance
        int shadowResistanceReturned = entity.currentShadowResistance;
        Debug.Log(entity.name + " base shadow resistance: " + shadowResistanceReturned.ToString());

        // Check Shadow Form passive
        if (entity.myPassiveManager.shadowForm)
        {
            shadowResistanceReturned += 30;
            Debug.Log("Value after 'Shadow Form' passive bonus: " + shadowResistanceReturned.ToString());
        }

        // return final value
        return shadowResistanceReturned;
    }
    private static int GetTotalFrostResistance(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetTotalFrostResistance() called for " + entity.name + "...");

        // Get base resistance
        int frostResistanceReturned = entity.currentFrostResistance;
        Debug.Log(entity.name + " base frost resistance: " + frostResistanceReturned.ToString());

        // Check Frozen Heart passive
        if (entity.myPassiveManager.frozenHeart)
        {
            frostResistanceReturned += 30;
            Debug.Log("Value after 'Frozen Heart' passive bonus: " + frostResistanceReturned.ToString());
        }

        // return final value
        return frostResistanceReturned;
    }
    private static int GetTotalPoisonResistance(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetTotalPoisonResistance() called for " + entity.name + "...");

        // Get base resistance
        int poisonResistanceReturned = entity.currentPoisonResistance;
        Debug.Log(entity.name + " base poison resistance: " + poisonResistanceReturned.ToString());

        // Check Toxicity passive
        if (entity.myPassiveManager.toxicity)
        {
            poisonResistanceReturned += 30;
            Debug.Log("Value after 'Toxicity' passive bonus: " + poisonResistanceReturned.ToString());
        }

        // return final value
        return poisonResistanceReturned;
    }
    private static int GetTotalAirResistance(LivingEntity entity)
    {
        Debug.Log("EntityLogic.GetTotalAirResistance() called for " + entity.name + "...");

        // Get base resistance
        int airResistanceReturned = entity.currentAirResistance;
        Debug.Log(entity.name + " base air resistance: " + airResistanceReturned.ToString());

        // Check Storm Lord passive
        if (entity.myPassiveManager.stormLord)
        {
            airResistanceReturned += 30;
            Debug.Log("Value after 'Storm Lord' passive bonus: " + airResistanceReturned.ToString());
        }

        // return final value
        return airResistanceReturned;
    }

    #endregion

    // Uniqie Enemy AI Ability + Targetting Logic
    public static LivingEntity GetBestInvigorateTarget(LivingEntity caster)
    {
        LivingEntity bestTarget = null;

        foreach (Enemy enemy in EnemyManager.Instance.allEnemies)
        {
            if (enemy.currentEnergy < enemy.currentMaxEnergy &&
                enemy != caster)
            {
                bestTarget = enemy;
            }
        }

        if (bestTarget == null)
        {
            Debug.Log("GetBestInvigorateTarget() is null, returning caster as best target");
            bestTarget = caster;
        }
        else if (bestTarget)
        {
            Debug.Log("GetBestInvigorateTarget() return character: " + bestTarget.name);
        }
        
        return bestTarget;
    }
    public static LivingEntity GetBestFortifyTarget(LivingEntity caster)
    {
        LivingEntity bestTarget = null;
        int score = 0;

        foreach (Enemy enemy in EnemyManager.Instance.allEnemies)
        {
            if (enemy.myPassiveManager.stunned &&
                score < 3 &&
                enemy != caster)
            {
                bestTarget = enemy;
                score = 3;
            }
            else if (enemy.myPassiveManager.sleep &&
                score < 2 &&
                enemy != caster)
            {
                bestTarget = enemy;
                score = 1;
            }
            else if (enemy.myPassiveManager.immobilized &&
                score < 1 &&
                enemy != caster)
            {
                bestTarget = enemy;
                score = 1;
            }
        }

        if (bestTarget != null)
        {
            Debug.Log("GetBestFortifyTarget() returned character: " + bestTarget.name);
        }

        return bestTarget;
    }
    public static LivingEntity GetBestChemicalReactionTarget(LivingEntity caster)
    {
        LivingEntity bestTarget = null;
        int highestPoisonStack = 0;

        foreach (LivingEntity entity in LivingEntityManager.Instance.allLivingEntities)
        {
            if (CombatLogic.Instance.IsTargetFriendly(caster, entity) == false &&
                entity.myPassiveManager.poisoned &&
                entity.myPassiveManager.poisonedStacks > highestPoisonStack &&
                entity.myPassiveManager.poisonedStacks > 5)
            {
                highestPoisonStack = entity.myPassiveManager.poisonedStacks;
                bestTarget = entity;
            }
        }

        if (bestTarget)
        {
            Debug.Log("EntityLogic.GetBestChemicalReactionTarget() returned character: " + bestTarget.myName);
        }
       
        return bestTarget;
    }
    public static LivingEntity GetBestDrainTarget(LivingEntity caster)
    {
        LivingEntity bestTarget = null;
        int highestPoisonStack = 0;

        foreach (LivingEntity entity in LivingEntityManager.Instance.allLivingEntities)
        {
            if (CombatLogic.Instance.IsTargetFriendly(caster, entity) == false &&
                entity.myPassiveManager.poisoned &&
                entity.myPassiveManager.poisonedStacks > highestPoisonStack &&
                entity.myPassiveManager.poisonedStacks > 12)
            {
                highestPoisonStack = entity.myPassiveManager.poisonedStacks;
                bestTarget = entity;
            }
        }

        if (bestTarget)
        {
            Debug.Log("EntityLogic.GetBestDrainTarget() return character: " + bestTarget.name);
        }
       
        return bestTarget;
    }
    public static Tile GetBestSummonEnemyLocation(LivingEntity caster, LivingEntity target, int range)
    {
        Debug.Log("EntityLogic.GetBestSummonEnemyLocation() called, comparing caster " + caster.myName +
            " to target " + target.myName + " with a range of: " + range.ToString());

        // Set up
        Tile tileReturned = null;
        List<Tile> allPossibleSpawnLocations = LevelManager.Instance.GetValidMoveableTilesWithinRange(range, caster.tile);
        List<Tile> finalList = new List<Tile>();

        // if target is to the left
        if (target.gridPosition.X <= caster.gridPosition.X)
        {
            foreach (Tile tile in allPossibleSpawnLocations)
            {
                // filter to only tiles in between caster and target
                if (tile.GridPosition.X >= target.gridPosition.X && tile.GridPosition.X <= caster.gridPosition.X)
                {
                    finalList.Add(tile);
                }
            }
        }

        // if target is to the right
        else if (target.gridPosition.X > caster.gridPosition.X)
        {
            foreach (Tile tile in allPossibleSpawnLocations)
            {
                // filter to only tiles in between caster and target
                if (tile.GridPosition.X <= target.gridPosition.X && tile.GridPosition.X >= caster.gridPosition.X)
                {
                    finalList.Add(tile);
                }
            }
        }        

        // print ideal tiles count
        Debug.Log("EntityLogic.GetBestSummonEnemyLocation() found " + finalList.Count.ToString() + " ideal spawn locations");

        // Get closest valid spawn location to target
        tileReturned = LevelManager.Instance.GetClosestValidTile(finalList, target.tile);

        // print tile info
        if(tileReturned != null)
        {
            Debug.Log("EntityLogic.GetBestSummonEnemyLocation() return tile " + tileReturned.GridPosition.X.ToString() + ", "
                + tileReturned.GridPosition.Y.ToString());
        }

        // return
        return tileReturned;
    }
    public static LivingEntity GetBestMeltTarget(LivingEntity caster)
    {
        LivingEntity bestTarget = null;
        int highestBlockValue = 0;

        foreach (LivingEntity entity in LivingEntityManager.Instance.allLivingEntities)
        {
            if (CombatLogic.Instance.IsTargetFriendly(caster, entity) == false &&
                entity.currentBlock > 0 &&
                entity.currentBlock > highestBlockValue)
            {
                highestBlockValue = entity.currentBlock;
                bestTarget = entity;
            }
        }

        if (bestTarget)
        {
            Debug.Log("EntityLogic.GetBestMeltTarget() returned character: " + bestTarget.myName);
        }

        return bestTarget;
    }
    public static LivingEntity GetBestCombustionTarget(LivingEntity caster)
    {
        LivingEntity bestTarget = null;
        int highestBurningValue = 0;

        foreach (LivingEntity entity in LivingEntityManager.Instance.allLivingEntities)
        {
            if (CombatLogic.Instance.IsTargetFriendly(caster, entity) == false &&
                entity.myPassiveManager.burningStacks > 0 &&
                entity.myPassiveManager.burningStacks > highestBurningValue)
            {
                highestBurningValue = entity.myPassiveManager.burningStacks;
                bestTarget = entity;
            }
        }

        if (bestTarget)
        {
            Debug.Log("EntityLogic.GetBestMeltTarget() returned character: " + bestTarget.myName);
        }

        return bestTarget;
    }
}

