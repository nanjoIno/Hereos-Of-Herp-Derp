﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityLogic : MonoBehaviour
{
    // Singleton model   

    public static AbilityLogic Instance;
    private void Awake()
    {
        Instance = this;
    }

    // Misc Logic
    public void OnAbilityUsed(Ability ability, LivingEntity livingEntity)
    {
        Debug.Log("OnAbilityUsed() called for " + livingEntity.gameObject.name + " using " + ability.abilityName);

        // Disable tile hover + tile highlights
        TileHover.Instance.SetVisibility(false);
        LevelManager.Instance.UnhighlightAllTiles();

        // temp variables
        int finalApCost = ability.abilityAPCost;
        int finalCD = ability.abilityBaseCooldownTime;
        // Set ability on cooldown
        

        // Reduce AP by cost of the ability
        // check for preparation here
        if (livingEntity.myPassiveManager.Preparation && ability.abilityName != "Preparation" && ability.abilityName != "Slice And Dice")
        {
            livingEntity.myPassiveManager.Preparation = false;
            livingEntity.myPassiveManager.preparationStacks = 0;
            livingEntity.myStatusManager.RemoveStatusIcon(livingEntity.myStatusManager.GetStatusIconByName("Preparation"));
            finalApCost = 0;
        }        


        // TO DO: re-do fleetfooted pasive bonus logic: move ability should be free, not paid for then refunded with AP
        if(ability.abilityName == "Move")
        {
            // if character has a free move available
            if (livingEntity.moveActionsTakenThisTurn == 0 && livingEntity.myPassiveManager.FleetFooted)
            {
                livingEntity.StartCoroutine(VisualEffectManager.Instance.CreateStatusEffect(livingEntity.transform.position, "Fleet Footed", true, "Blue"));
                finalApCost = 0;
            }
            livingEntity.moveActionsTakenThisTurn++;
        }

        else if (ability.abilityName == "Slice And Dice")
        {
            finalApCost = livingEntity.currentAP;
        }

        // Modify AP
        livingEntity.ModifyCurrentAP(-finalApCost);
        // Modify Cooldown
        ability.ModifyCurrentCooldown(finalCD);

        
    }

    // Abilities

    // Free Strike
    public Action PerformFreeStrike(LivingEntity attacker, LivingEntity victim)
    {
        Action action = new Action();
        StartCoroutine(PerformFreeStrikeCoroutine(attacker, victim, action));
        return action;
    }
    public IEnumerator PerformFreeStrikeCoroutine(LivingEntity attacker, LivingEntity victim, Action action)
    {
        StartCoroutine(VisualEffectManager.Instance.CreateStatusEffect(attacker.transform.position, "Free Strike", false, "Blue"));
        yield return new WaitForSeconds(0.5f);
        // to do: this should
        // AbilityLogic.Instance.PerformFreeStrike
        // wait until this is resolved
        // continue
        Ability strike = attacker.mySpellBook.GetAbilityByName("Strike");
        attacker.StartCoroutine(attacker.AttackMovement(victim));
        Action abilityAction = CombatLogic.Instance.HandleDamage(strike.abilityPrimaryValue, attacker, victim, false, strike.abilityAttackType, strike.abilityDamageType);
        yield return new WaitUntil(() => abilityAction.ActionResolved() == true);
        yield return new WaitForSeconds(1f);
        action.actionResolved = true;        
    }
    //Strike
    public Action PerformStrike(LivingEntity attacker, LivingEntity victim)
    {
        Action action = new Action();
        StartCoroutine(PerformStrikeCoroutine(attacker, victim, action));
        return action;
    }
    public IEnumerator PerformStrikeCoroutine(LivingEntity attacker, LivingEntity victim, Action action)
    {
        Ability strike = attacker.mySpellBook.GetAbilityByName("Strike");
        OnAbilityUsed(strike, attacker);
        attacker.StartCoroutine(attacker.AttackMovement(victim));        
        Action abilityAction = CombatLogic.Instance.HandleDamage(strike.abilityPrimaryValue, attacker, victim, false, strike.abilityAttackType, strike.abilityDamageType);
        yield return new WaitUntil(() => abilityAction.ActionResolved() == true);
        action.actionResolved = true;
        
    }

    // Twin Strike
    public Action PerformTwinStrike(LivingEntity attacker, LivingEntity victim)
    {
        Action action = new Action();
        StartCoroutine(PerformTwinStrikeCoroutine(attacker, victim, action));
        return action;
    }
    public IEnumerator PerformTwinStrikeCoroutine(LivingEntity attacker, LivingEntity victim, Action action)
    {
        Ability twinStrike = attacker.mySpellBook.GetAbilityByName("Twin Strike");
        OnAbilityUsed(twinStrike, attacker);
        StartCoroutine(attacker.AttackMovement(victim));
        Action abilityAction = CombatLogic.Instance.HandleDamage(twinStrike.abilityPrimaryValue, attacker, victim, false, twinStrike.abilityAttackType, twinStrike.abilityDamageType);
        yield return new WaitUntil(() => abilityAction.ActionResolved() == true);
        yield return new WaitForSeconds(0.3f);
        // check to make sure the enemy wasnt killed by the first attack
        if (victim.currentHealth > 0 && victim != null)
        {
            StartCoroutine(attacker.AttackMovement(victim));
            Action abilityAction2 = CombatLogic.Instance.HandleDamage(twinStrike.abilityPrimaryValue, attacker, victim, false, twinStrike.abilityAttackType, twinStrike.abilityDamageType);
            yield return new WaitUntil(() => abilityAction2.ActionResolved() == true);
        }

        action.actionResolved = true;
    }

    // Fire Ball
    public Action PerformFireBall(LivingEntity attacker, LivingEntity victim)
    {
        Action action = new Action();
        StartCoroutine(PerformFireBallCoroutine(attacker, victim, action));
        return action;
    }
    public IEnumerator PerformFireBallCoroutine(LivingEntity attacker, LivingEntity victim, Action action)
    {
        Ability fireball = attacker.mySpellBook.GetAbilityByName("Fire Ball");
        OnAbilityUsed(fireball, attacker);
        attacker.StartCoroutine(attacker.AttackMovement(victim));
        Action abilityAction = CombatLogic.Instance.HandleDamage(fireball.abilityPrimaryValue, attacker, victim, false, fireball.abilityAttackType, fireball.abilityDamageType, fireball);
        // check for improved fireball talent
        if(attacker.defender && attacker.defender.myCharacterData.KnowsImprovedFireBall)
        {
            victim.myPassiveManager.ModifyIgnite(1);
        }
        yield return new WaitUntil(() => abilityAction.ActionResolved() == true);

        action.actionResolved = true;
        
    }

    // Chaos Bolt
    public void PerformChaosBolt(LivingEntity attacker, LivingEntity victim)
    {
        StartCoroutine(PerformChaosBoltCoroutine(attacker, victim));
    }
    public IEnumerator PerformChaosBoltCoroutine(LivingEntity attacker, LivingEntity victim)
    {
        Ability chaosBolt = attacker.mySpellBook.GetAbilityByName("Chaos Bolt");
        OnAbilityUsed(chaosBolt, attacker);
        attacker.StartCoroutine(attacker.AttackMovement(victim));
        Action abilityAction = CombatLogic.Instance.HandleDamage(chaosBolt.abilityPrimaryValue, attacker, victim, false, chaosBolt.abilityAttackType, chaosBolt.abilityDamageType);
        yield return new WaitUntil(() => abilityAction.ActionResolved() == true);
        victim.myPassiveManager.ModifyExposed(chaosBolt.abilitySecondaryValue);
        
        yield return null;
    }

    // Snipe
    public void PerformSnipe(LivingEntity attacker, LivingEntity victim)
    {
        StartCoroutine(PerformSnipeCoroutine(attacker, victim));
    }
    public IEnumerator PerformSnipeCoroutine(LivingEntity attacker, LivingEntity victim)
    {
        Ability snipe = attacker.mySpellBook.GetAbilityByName("Snipe");
        OnAbilityUsed(snipe, attacker);
        StartCoroutine(attacker.AttackMovement(victim));
        Action abilityAction = CombatLogic.Instance.HandleDamage(snipe.abilityPrimaryValue, attacker, victim, false, snipe.abilityAttackType, snipe.abilityDamageType);
        yield return new WaitUntil(() => abilityAction.ActionResolved() == true);
        
        yield return null;
    }

    // Inspire
    public Action PerformInspire(LivingEntity caster, LivingEntity target)
    {
        Action action = new Action();
        StartCoroutine(PerformInspireCoroutine(caster, target, action));
        return action;
    }
    public IEnumerator PerformInspireCoroutine(LivingEntity caster, LivingEntity target, Action action)
    {
        Ability inspire = caster.mySpellBook.GetAbilityByName("Inspire");
        OnAbilityUsed(inspire, caster);
        target.ModifyCurrentStrength(inspire.abilityPrimaryValue);
        if(caster.defender && caster.defender.myCharacterData.KnowsImprovedInspire)
        {
            target.ModifyCurrentDexterity(inspire.abilityPrimaryValue);
            target.ModifyCurrentWisdom(inspire.abilityPrimaryValue);
        }
        action.actionResolved = true;
        yield return null;
    }

    // Get Down!
    public Action PerformGetDown(LivingEntity caster, TileScript destination)
    {
        Action action = new Action();
        StartCoroutine(PerformGetDownCoroutine(caster, destination, action));
        return action;
    }
    public IEnumerator PerformGetDownCoroutine(LivingEntity caster, TileScript destination, Action action)
    {  
        Ability getDown = caster.mySpellBook.GetAbilityByName("Get Down!");
        OnAbilityUsed(getDown, caster);
        Action moveAction = MovementLogic.Instance.MoveEntity(caster, destination, 5);

        // yield wait until movement complete
        yield return new WaitUntil(() => moveAction.ActionResolved() == true);

        // Give adjacent characters block
        List<TileScript> adjacentTiles = LevelManager.Instance.GetTilesWithinRange(1, destination);
        foreach (LivingEntity livingEntity in LivingEntityManager.Instance.allLivingEntities)
        {
            if (adjacentTiles.Contains(livingEntity.TileCurrentlyOn) &&
                CombatLogic.Instance.IsTargetFriendly(caster, livingEntity)
                )
            {
                livingEntity.ModifyCurrentBlock(getDown.abilityPrimaryValue);
            }
        }

        action.actionResolved = true;
    }

    // Smash
    public void PerformSmash(LivingEntity attacker, LivingEntity victim)
    {
        StartCoroutine(PerformSmashCoroutine(attacker, victim));
    }
    public IEnumerator PerformSmashCoroutine(LivingEntity attacker, LivingEntity victim)
    {
        Debug.Log("Performing Smash...");
        Ability smash = attacker.mySpellBook.GetAbilityByName("Smash");
        OnAbilityUsed(smash, attacker);
        // Attack
        StartCoroutine(attacker.AttackMovement(victim));
        Action abilityAction = CombatLogic.Instance.HandleDamage(smash.abilityPrimaryValue, attacker, victim, false, smash.abilityAttackType, smash.abilityDamageType);
        yield return new WaitUntil(() => abilityAction.ActionResolved() == true);

        // Knock back.
        MovementLogic.Instance.KnockBackEntity(attacker, victim, smash.abilitySecondaryValue);
        
        yield return null;
    }

    // Chain Lightning
    public void PerformChainLightning(LivingEntity attacker, LivingEntity target)
    {
        StartCoroutine(PerformChainLightningCoroutine(attacker, target));
    }
    public IEnumerator PerformChainLightningCoroutine(LivingEntity attacker, LivingEntity victim)
    {
        Debug.Log("Performing Chain Lightning...");
        Ability chainLightning = attacker.mySpellBook.GetAbilityByName("Chain Lightning");
        OnAbilityUsed(chainLightning, attacker);
        StartCoroutine(attacker.AttackMovement(victim));

        LivingEntity currentTarget = victim;
        LivingEntity previousTarget = victim;

        Action abilityAction = CombatLogic.Instance.HandleDamage(chainLightning.abilityPrimaryValue, attacker, victim, false, chainLightning.abilityAttackType, chainLightning.abilityDamageType);
        yield return new WaitUntil(() => abilityAction.ActionResolved() == true);
        yield return new WaitForSeconds(0.2f);

        for (int lightningJumps = 0; lightningJumps < chainLightning.abilitySecondaryValue; lightningJumps++)
        {
            List<TileScript> adjacentTiles = LevelManager.Instance.GetTilesWithinRange(1, currentTarget.TileCurrentlyOn);

            foreach (Enemy enemy in EnemyManager.Instance.allEnemies)
            {
                if (adjacentTiles.Contains(enemy.TileCurrentlyOn))
                {
                    previousTarget = currentTarget;
                    currentTarget = enemy;
                }
            }

            if (previousTarget != currentTarget)
            {
                Action abilityAction2 = CombatLogic.Instance.HandleDamage(chainLightning.abilityPrimaryValue, attacker, victim, false, chainLightning.abilityAttackType, chainLightning.abilityDamageType);
                yield return new WaitUntil(() => abilityAction2.ActionResolved() == true);
                yield return new WaitForSeconds(0.2f);
            }

        }

    }

    // Primal Blast
    public void PerformPrimalBlast(LivingEntity attacker, LivingEntity victim)
    {
        StartCoroutine(PerformPrimalBlastCoroutine(attacker, victim));
    }
    public IEnumerator PerformPrimalBlastCoroutine(LivingEntity attacker, LivingEntity target)
    {
        Debug.Log("Performing Primal Blast...");
        Ability strike = attacker.mySpellBook.GetAbilityByName("Primal Blast");
        OnAbilityUsed(strike, attacker);
        StartCoroutine(attacker.AttackMovement(target));

        Action abilityAction = CombatLogic.Instance.HandleDamage(strike.abilityPrimaryValue, attacker, target, false, strike.abilityAttackType, AbilityDataSO.DamageType.Physical);
        yield return new WaitUntil(() => abilityAction.ActionResolved() == true);
        yield return new WaitForSeconds(0.2f);

        Action abilityAction2 = CombatLogic.Instance.HandleDamage(strike.abilityPrimaryValue, attacker, target, false, strike.abilityAttackType, AbilityDataSO.DamageType.Magic);
        yield return new WaitUntil(() => abilityAction2.ActionResolved() == true);        

        
    }

    // Meteor
    public void PerformMeteor(LivingEntity attacker, TileScript location)
    {
        StartCoroutine(PerformMeteorCoroutine(attacker, location));
    }
    public IEnumerator PerformMeteorCoroutine(LivingEntity attacker, TileScript location)
    {
        Ability meteor = attacker.mySpellBook.GetAbilityByName("Meteor");
        OnAbilityUsed(meteor, attacker);
        CombatLogic.Instance.CreateAoEAttackEvent(attacker, meteor, location, 1, false, true);
        
        yield return null;
    }

    // Whirlwind
    public Action PerformWhirlwind(LivingEntity attacker)
    {
        Action action = new Action();
        StartCoroutine(PerformWhirlwindCoroutine(attacker, action));
        return action;
    }
    public IEnumerator PerformWhirlwindCoroutine(LivingEntity attacker, Action action)
    {
        Ability whirlwind = attacker.mySpellBook.GetAbilityByName("Whirlwind");
        OnAbilityUsed(whirlwind, attacker);
        CombatLogic.Instance.CreateAoEAttackEvent(attacker, whirlwind, attacker.TileCurrentlyOn, 1, true, true);

        // Improved Whirlwind talent
        if(attacker.defender != null)
        {
            if (attacker.defender.myCharacterData.KnowsImprovedWhirlwind)
            {
                List<TileScript> tilesInWhirlwindRange = LevelManager.Instance.GetTilesWithinRange(1, attacker.TileCurrentlyOn);
                foreach (LivingEntity entity in LivingEntityManager.Instance.allLivingEntities)
                {
                    if (CombatLogic.Instance.IsTargetFriendly(attacker, entity) == false &&
                        tilesInWhirlwindRange.Contains(entity.TileCurrentlyOn))
                    {
                        entity.myPassiveManager.ModifyExhausted(1);
                        entity.myPassiveManager.ModifyExposed(1);
                    }
                }
            }
            
        }
        yield return new WaitForSeconds(1f);
        action.actionResolved = true;
        
    }
    // Frost Nova
    public Action PerformFrostNova(LivingEntity attacker)
    {
        Action action = new Action();
        StartCoroutine(PerformFrostNovaCoroutine(attacker, action));
        return action;
    }
    public IEnumerator PerformFrostNovaCoroutine(LivingEntity attacker, Action action)
    {
        Ability frostNova = attacker.mySpellBook.GetAbilityByName("Frost Nova");
        OnAbilityUsed(frostNova, attacker);

        // damage enmies within 1
        CombatLogic.Instance.CreateAoEAttackEvent(attacker, frostNova, attacker.TileCurrentlyOn, 1, true, false);

        // pin enemies within 1
        List<TileScript> tilesInNovaRange = LevelManager.Instance.GetTilesWithinRange(1, attacker.TileCurrentlyOn);
        foreach(LivingEntity entity in LivingEntityManager.Instance.allLivingEntities)
        {
            if(!CombatLogic.Instance.IsTargetFriendly(attacker, entity) && tilesInNovaRange.Contains(entity.TileCurrentlyOn))
            {
                entity.ApplyPinned();
            }
        }
        
        yield return new WaitForSeconds(1f);
        action.actionResolved = true;

    }
    public Action PerformBloodLust(LivingEntity attacker)
    {
        Action action = new Action();
        StartCoroutine(PerformBloodLustCoroutine(attacker, action));
        return action;
    }
    public IEnumerator PerformBloodLustCoroutine(LivingEntity attacker, Action action)
    {
        Ability bloodLust = attacker.mySpellBook.GetAbilityByName("Blood Lust");
        OnAbilityUsed(bloodLust, attacker);
        Action selfDamageAction = CombatLogic.Instance.HandleDamage(bloodLust.abilitySecondaryValue, attacker, attacker, false, AbilityDataSO.AttackType.None, AbilityDataSO.DamageType.None);
        yield return new WaitUntil(() => selfDamageAction.ActionResolved() == true);
        yield return new WaitForSeconds(0.5f);
        attacker.ModifyCurrentAP(bloodLust.abilityPrimaryValue);
        action.actionResolved = true;        
    }

    // Guard
    public void PerformGuard(LivingEntity caster, LivingEntity target)
    {
        StartCoroutine(PerformGuardCoroutine(caster, target));
    }
    public IEnumerator PerformGuardCoroutine(LivingEntity caster,LivingEntity target)
    {
        Ability guard = caster.mySpellBook.GetAbilityByName("Guard");
        OnAbilityUsed(guard, caster);
        target.ModifyCurrentBarrierStacks(guard.abilityPrimaryValue);
        
        yield return null;
    }

    // Frost bolt
    public void PerformFrostBolt(LivingEntity caster, LivingEntity victim)
    {
        StartCoroutine(PerformFrostBoltCoroutine(caster, victim));
    }
    public IEnumerator PerformFrostBoltCoroutine(LivingEntity attacker, LivingEntity victim)
    {
        Ability frostbolt = attacker.mySpellBook.GetAbilityByName("Frost Bolt");
        OnAbilityUsed(frostbolt, attacker);
        attacker.StartCoroutine(attacker.AttackMovement(victim));
        Action abilityAction = CombatLogic.Instance.HandleDamage(frostbolt.abilityPrimaryValue, attacker, victim, false, frostbolt.abilityAttackType, frostbolt.abilityDamageType);
        yield return new WaitUntil(() => abilityAction.ActionResolved() == true);
        victim.ApplyPinned(attacker);
        
    }

    // Shoot
    public void PerformShoot(LivingEntity attacker, LivingEntity victim)
    {
        StartCoroutine(PerformShootCoroutine(attacker, victim));
    }
    public IEnumerator PerformShootCoroutine(LivingEntity attacker, LivingEntity victim)
    {
        Ability shoot = attacker.mySpellBook.GetAbilityByName("Shoot");
        OnAbilityUsed(shoot, attacker);
        Action abilityAction = CombatLogic.Instance.HandleDamage(shoot.abilityPrimaryValue, attacker, victim, false, shoot.abilityAttackType, shoot.abilityDamageType);
        yield return new WaitUntil(() => abilityAction.ActionResolved() == true);
      
    }

    // Rapid Fire
    public void PerformRapidFire(LivingEntity attacker, LivingEntity victim, int shots)
    {
        StartCoroutine(PerformRapidFireCoroutine(attacker, victim, shots));
    }
    public IEnumerator PerformRapidFireCoroutine(LivingEntity attacker, LivingEntity victim, int shots)
    {
        Ability rapidFire = attacker.mySpellBook.GetAbilityByName("Rapid Fire");
        OnAbilityUsed(rapidFire, attacker);

        int shotsFired = 0;

        ShotStart:
        Action abilityAction = CombatLogic.Instance.HandleDamage(rapidFire.abilityPrimaryValue, attacker, victim, false, rapidFire.abilityAttackType, rapidFire.abilityDamageType);
        yield return new WaitUntil(() => abilityAction.ActionResolved() == true);
        shotsFired++;
        attacker.ModifyCurrentAP(-1);
        yield return new WaitForSeconds(0.5f);
        if (victim != null && shotsFired < shots)
        {
            goto ShotStart;
        }
        
    }

    // Poison Dart
    public void PerformPoisonDart(LivingEntity caster, LivingEntity victim)
    {
        StartCoroutine(PerformPoisonDartCoroutine(caster, victim));
    }
    public IEnumerator PerformPoisonDartCoroutine(LivingEntity caster, LivingEntity victim)
    {
        Ability poisonDart = caster.mySpellBook.GetAbilityByName("Poison Dart");
        OnAbilityUsed(poisonDart, caster);
        StartCoroutine(caster.AttackMovement(victim));        
        victim.ModifyPoison(poisonDart.abilitySecondaryValue, caster);
        
        yield return null;
    }

    // Chemical Reaction
    public Action PerformChemicalReaction(LivingEntity caster, LivingEntity victim)
    {
        Action action = new Action();
        StartCoroutine(PerformChemicalReactionCoroutine(caster, victim, action));
        return action;
    }
    public IEnumerator PerformChemicalReactionCoroutine(LivingEntity caster, LivingEntity victim, Action action)
    {
        Ability chemicalReaction = caster.mySpellBook.GetAbilityByName("Chemical Reaction");
        OnAbilityUsed(chemicalReaction, caster);
        StartCoroutine(caster.AttackMovement(victim));
        victim.ModifyPoison(victim.poisonStacks, caster);
        
        yield return null;
    }

    // Slice And Dice
    public Action PerformSliceAndDice(LivingEntity caster, LivingEntity victim)
    {
        Action action = new Action();
        StartCoroutine(PerformSliceAndDiceCoroutine(caster, victim, action));
        return action;
    }
    public IEnumerator PerformSliceAndDiceCoroutine(LivingEntity attacker, LivingEntity victim,  Action action)
    {
        int totalAttacks = attacker.currentAP;
        int timesAttacked = 0;
        Ability sliceAndDice = attacker.mySpellBook.GetAbilityByName("Slice And Dice");
        OnAbilityUsed(sliceAndDice, attacker);        

        AttackLoopStart:
        StartCoroutine(attacker.AttackMovement(victim));
        Action abilityAction = CombatLogic.Instance.HandleDamage(sliceAndDice.abilityPrimaryValue, attacker, victim, false, sliceAndDice.abilityAttackType, sliceAndDice.abilityDamageType);
        yield return new WaitUntil(() => abilityAction.ActionResolved() == true);
        yield return new WaitForSeconds(0.3f);
        timesAttacked++;
        if (victim.inDeathProcess == false && victim != null && timesAttacked < totalAttacks)
        {
            goto AttackLoopStart;
        }
        action.actionResolved = true;

        
    }

    // Impaling Bolt
    public void PerformImpalingBolt(LivingEntity caster, LivingEntity victim)
    {
        StartCoroutine(PerformImpalingBoltCoroutine(caster, victim));
    }
    public IEnumerator PerformImpalingBoltCoroutine(LivingEntity attacker, LivingEntity victim)
    {
        Ability impalingBolt = attacker.mySpellBook.GetAbilityByName("Impaling Bolt");
        OnAbilityUsed(impalingBolt, attacker);

        // Attack
        StartCoroutine(attacker.AttackMovement(victim));
        Action abilityAction = CombatLogic.Instance.HandleDamage(impalingBolt.abilityPrimaryValue, attacker, victim, false, impalingBolt.abilityAttackType, impalingBolt.abilityDamageType);
        yield return new WaitUntil(() => abilityAction.ActionResolved() == true);

        // Knockback
        MovementLogic.Instance.KnockBackEntity(attacker, victim, impalingBolt.abilitySecondaryValue);
        
        yield return null;
    }

    // Rock Toss
    public void PerformRockToss(LivingEntity caster, LivingEntity victim)
    {
        StartCoroutine(PerformRockTossCoroutine(caster, victim));
    }
    public IEnumerator PerformRockTossCoroutine(LivingEntity attacker, LivingEntity victim)
    {
        Ability rockToss = attacker.mySpellBook.GetAbilityByName("Rock Toss");
        OnAbilityUsed(rockToss, attacker);

        // Attack
        StartCoroutine(attacker.AttackMovement(victim));
        Action abilityAction = CombatLogic.Instance.HandleDamage(rockToss.abilityPrimaryValue, attacker, victim, false, rockToss.abilityAttackType, rockToss.abilityDamageType);
        yield return new WaitUntil(() => abilityAction.ActionResolved() == true);
        // Knockback
        MovementLogic.Instance.KnockBackEntity(attacker, victim, rockToss.abilitySecondaryValue);
        
    }

    // Invigorate
    public void PerformInvigorate(LivingEntity caster, LivingEntity target)
    {
        StartCoroutine(PerformInvigorateCoroutine(caster, target));
    }
    public IEnumerator PerformInvigorateCoroutine(LivingEntity caster, LivingEntity target)
    {
        Ability invigorate = caster.mySpellBook.GetAbilityByName("Invigorate");
        OnAbilityUsed(invigorate, caster);
        target.ModifyCurrentAP(invigorate.abilityPrimaryValue);
        if (TalentLogic.Instance.DoesCharacterHaveTalent(caster, "Improved Invigorate"))
        {
            target.myPassiveManager.ModifyTemporaryMobility(1);
        }
        ParticleManager.Instance.CreateParticleEffect(target.TileCurrentlyOn.WorldPosition, ParticleManager.Instance.basicParticlePrefab);
        yield return null;
      
    }

    // Lightning Shield
    public void PerformLightningShield(LivingEntity caster, LivingEntity target)
    {
        StartCoroutine(PerformLightningShieldCoroutine(caster, target));
    }
    public IEnumerator PerformLightningShieldCoroutine(LivingEntity caster, LivingEntity target)
    {
        Ability lightningShield = caster.mySpellBook.GetAbilityByName("Lightning Shield");
        OnAbilityUsed(lightningShield, caster);
        target.myPassiveManager.LearnLightningShield(lightningShield.abilityPrimaryValue);
        VisualEffectManager.Instance.CreateStatusEffect(target.transform.position, "Lightning Shield", false, "Blue");
        
        yield return null;
    }

    // Holy Fire
    public void PerformHolyFire(LivingEntity caster, LivingEntity target)
    {
        StartCoroutine(PerformHolyFireCoroutine(caster, target));
    }
    public IEnumerator PerformHolyFireCoroutine(LivingEntity caster, LivingEntity target)
    {
        Ability holyFire = caster.mySpellBook.GetAbilityByName("Holy Fire");
        OnAbilityUsed(holyFire, caster);

        if (CombatLogic.Instance.IsTargetFriendly(caster, target))
        {
            target.ModifyCurrentBlock(holyFire.abilityPrimaryValue);
        }
        else
        {
            Action abilityAction = CombatLogic.Instance.HandleDamage(holyFire.abilityPrimaryValue, caster, target, false, holyFire.abilityAttackType, holyFire.abilityDamageType);
            yield return new WaitUntil(() => abilityAction.ActionResolved() == true);
        }

    }

    // Primal Rage
    public void PerformPrimalRage(LivingEntity caster, LivingEntity target)
    {
        StartCoroutine(PerformPrimalRageCoroutine(caster, target));
    }
    public IEnumerator PerformPrimalRageCoroutine(LivingEntity caster, LivingEntity target)
    {
        Ability primalRage = caster.mySpellBook.GetAbilityByName("Primal Rage");
        OnAbilityUsed(primalRage, caster);
        target.ModifyCurrentStrength(primalRage.abilityPrimaryValue);
        target.myPassiveManager.ModifyTemporaryStrength(primalRage.abilityPrimaryValue);

        yield return null;
    }

    // Phase Shift
    public Action PerformPhaseShift(LivingEntity caster, LivingEntity target)
    {
        Action action = new Action();
        StartCoroutine(PerformPhaseShiftCoroutine(caster, target, action));
        return action;
    }
    public IEnumerator PerformPhaseShiftCoroutine(LivingEntity caster, LivingEntity target, Action action)
    {
        Ability phaseShift = caster.mySpellBook.GetAbilityByName("Phase Shift");
        OnAbilityUsed(phaseShift, caster);

        TileScript casterDestination = target.TileCurrentlyOn;

        MovementLogic.Instance.TeleportEntity(caster, target.TileCurrentlyOn, true);
        MovementLogic.Instance.TeleportEntity(target, casterDestination, true);

        action.actionResolved = true;

        
        yield return null;
        
    }

    // Siphon Life
    public void PerformSiphonLife(LivingEntity caster, LivingEntity target)
    {
        StartCoroutine(PerformSiphonLifeCoroutine(caster, target));
    }
    public IEnumerator PerformSiphonLifeCoroutine(LivingEntity caster, LivingEntity target)
    {
        Ability siphonLife = caster.mySpellBook.GetAbilityByName("Siphon Life");
        OnAbilityUsed(siphonLife, caster);
        target.ModifyCurrentStrength(-siphonLife.abilityPrimaryValue);
        caster.ModifyCurrentStrength(siphonLife.abilityPrimaryValue);
        
        yield return null;
    }

    // Sanctity
    public void PerformSanctity(LivingEntity caster, LivingEntity target)
    {
        StartCoroutine(PerformSanctityCoroutine(caster, target));
    }
    public IEnumerator PerformSanctityCoroutine(LivingEntity caster, LivingEntity target)
    {
        Ability sanctity = caster.mySpellBook.GetAbilityByName("Sanctity");
        OnAbilityUsed(sanctity, caster);

        if (target.isStunned)
        {
            VisualEffectManager.Instance.CreateStatusEffect(target.transform.position, "Stun Removed", false, "Blue");
            yield return new WaitForSeconds(0.2f);
            target.RemoveStunned();
        }
        if (target.isPinned)
        {
            VisualEffectManager.Instance.CreateStatusEffect(target.transform.position, "Pinned Removed", false, "Blue");
            yield return new WaitForSeconds(0.2f);
            target.RemovePinned();
        }
        if (target.isPoisoned)
        {
            VisualEffectManager.Instance.CreateStatusEffect(target.transform.position, "Poison Removed", false, "Blue");
            yield return new WaitForSeconds(0.2f);
            target.ModifyPoison(-target.poisonStacks);
        }
        // remove vulnerable
        // remove weakened

        
    }

    // Bless
    public Action PerformBless(LivingEntity caster, LivingEntity target)
    {
        Action action = new Action();
        StartCoroutine(PerformBlessCoroutine(caster, target, action));
        return action;
    }
    public IEnumerator PerformBlessCoroutine(LivingEntity caster, LivingEntity target, Action action)
    {
        Ability bless = caster.mySpellBook.GetAbilityByName("Bless");
        OnAbilityUsed(bless, caster);
        target.myPassiveManager.ModifyRune(bless.abilityPrimaryValue);
        yield return new WaitForSeconds(0.5f);
        action.actionResolved = true;        
    }

    // Void Bomb
    public void PerformVoidBomb(LivingEntity caster, LivingEntity target)
    {
        StartCoroutine(PerformVoidBombCoroutine(caster, target));
    }
    public IEnumerator PerformVoidBombCoroutine(LivingEntity attacker, LivingEntity victim)
    {
        Ability voidBomb = attacker.mySpellBook.GetAbilityByName("Void Bomb");
        OnAbilityUsed(voidBomb, attacker);
        Action abilityAction = CombatLogic.Instance.HandleDamage(voidBomb.abilityPrimaryValue, attacker, victim, false, voidBomb.abilityAttackType, voidBomb.abilityDamageType);
        yield return new WaitUntil(() => abilityAction.ActionResolved() == true);
        victim.ApplyStunned();
        
        yield return null;

    }

    // Nightmare
    public Action PerformNightmare(LivingEntity caster, LivingEntity target)
    {
        Action action = new Action();
        StartCoroutine(PerformNightmareCoroutine(caster, target, action));
        return action;
    }
    public IEnumerator PerformNightmareCoroutine(LivingEntity caster, LivingEntity target, Action action)
    {
        Ability nightmare = caster.mySpellBook.GetAbilityByName("Nightmare");
        OnAbilityUsed(nightmare, caster);
        target.ModifySleeping(nightmare.abilityPrimaryValue);
        yield return new WaitForSeconds(0.5f);
        action.actionResolved = true;        
    }

    // Move
    public Action PerformMove(LivingEntity characterMoved, TileScript destination)
    {
        Action action = new Action();
        StartCoroutine(PerformMoveCoroutine(characterMoved, destination, action));
        return action;
    }
    public IEnumerator PerformMoveCoroutine(LivingEntity characterMoved, TileScript destination, Action action)
    {
        Ability move = characterMoved.mySpellBook.GetAbilityByName("Move");
        OnAbilityUsed(move, characterMoved);
        Action movementAction = MovementLogic.Instance.MoveEntity(characterMoved, destination);

        yield return new WaitUntil(() => movementAction.ActionResolved() == true);
        action.actionResolved = true;  
    }

    // Dash
    public void PerformDash(LivingEntity characterMoved, TileScript destination)
    {
        StartCoroutine(PerformDashCoroutine(characterMoved, destination));
    }
    public IEnumerator PerformDashCoroutine(LivingEntity characterMoved, TileScript destination)
    {
        Ability dash = characterMoved.mySpellBook.GetAbilityByName("Dash");

        OnAbilityUsed(dash, characterMoved);

        Action dashAction = MovementLogic.Instance.MoveEntity(characterMoved, destination, 5);
        
        yield return new WaitUntil(() => dashAction.ActionResolved() == true);

        if (characterMoved.defender)
        {
            if (characterMoved.defender.myCharacterData.KnowsImprovedDash)
            {
                characterMoved.myPassiveManager.ModifyTemporaryStrength(2);
            }
        }

    }

    // Charge
    public Action PerformCharge(LivingEntity caster, LivingEntity target, TileScript destination)
    {
        Action action = new Action();
        StartCoroutine(PerformChargeCoroutine(caster, target, destination, action));
        return action;
    }
    public IEnumerator PerformChargeCoroutine(LivingEntity attacker, LivingEntity victim, TileScript destination, Action action)
    {
        Ability charge = attacker.mySpellBook.GetAbilityByName("Charge");
        OnAbilityUsed(charge, attacker);
        // Charge movement
        Action moveAction = MovementLogic.Instance.MoveEntity(attacker, destination, 5);

        // yield wait until movement complete
        yield return new WaitUntil(() => moveAction.ActionResolved() == true);

        // Charge attack
        attacker.StartCoroutine(attacker.AttackMovement(victim));
        Action abilityAction = CombatLogic.Instance.HandleDamage(charge.abilityPrimaryValue, attacker, victim, false, charge.abilityAttackType, charge.abilityDamageType);
        yield return new WaitUntil(() => abilityAction.ActionResolved() == true);
        // Apply exposed
        victim.myPassiveManager.ModifyExposed(charge.abilitySecondaryValue);

        action.actionResolved = true;
               
    }

    // Telekinesis
    public void PerformTelekinesis(LivingEntity caster, LivingEntity target, TileScript destination)
    {
        StartCoroutine(PerformTelekinesisCoroutine(caster, target, destination));
    }
    public IEnumerator PerformTelekinesisCoroutine(LivingEntity caster, LivingEntity target, TileScript destination)
    {
        Ability telekinesis = caster.mySpellBook.GetAbilityByName("Telekinesis");
        OnAbilityUsed(telekinesis, caster);

        MovementLogic.Instance.TeleportEntity(target, destination);

        

        yield return null;
    }

    // Teleport
    public void PerformTeleport(LivingEntity caster, TileScript destination)
    {
        StartCoroutine(PerformTeleportCoroutine(caster, destination));
    }
    public IEnumerator PerformTeleportCoroutine(LivingEntity caster, TileScript destination)
    {
        Ability teleport = caster.mySpellBook.GetAbilityByName("Teleport");
        OnAbilityUsed(teleport, caster);

        MovementLogic.Instance.TeleportEntity(caster, destination);

        

        yield return null;
    }

    // Doom
    public Action PerformDoom(LivingEntity caster)
    {
        Action action = new Action();
        StartCoroutine(PerformDoomCoroutine(caster, action));
        return action;
    }
    public IEnumerator PerformDoomCoroutine(LivingEntity caster, Action action)
    {
        Ability doom = caster.mySpellBook.GetAbilityByName("Doom");
        OnAbilityUsed(doom, caster);
        foreach (LivingEntity entity in LivingEntityManager.Instance.allLivingEntities)
        {
            if(CombatLogic.Instance.IsTargetFriendly(caster, entity) == false)
            {
                entity.ModifyCurrentEnergy(-1);
            }
        }
        yield return new WaitForSeconds(1f);
        action.actionResolved = true;
    }

    // Crushing Blow
    public Action PerformCrushingBlow(LivingEntity caster, LivingEntity target)
    {
        Action action = new Action();
        StartCoroutine(PerformCrushingBlowCoroutine(caster, target, action));
        return action;
    }
    public IEnumerator PerformCrushingBlowCoroutine(LivingEntity attacker, LivingEntity victim, Action action)
    {
        Ability crushingBlow = attacker.mySpellBook.GetAbilityByName("Crushing Blow");
        OnAbilityUsed(crushingBlow, attacker);
        attacker.StartCoroutine(attacker.AttackMovement(victim));

        Action abilityAction = CombatLogic.Instance.HandleDamage(crushingBlow.abilityPrimaryValue, attacker, victim, false, crushingBlow.abilityAttackType, crushingBlow.abilityDamageType);
        yield return new WaitUntil(() => abilityAction.ActionResolved() == true);
        
        victim.ApplyStunned();

        action.actionResolved = true;

    }

    // Summon Undead
    public void PerformSummonUndead(LivingEntity caster, LivingEntity target)
    {
        StartCoroutine(PerformSummonUndeadCoroutine(caster, target));
    }
    public IEnumerator PerformSummonUndeadCoroutine(LivingEntity caster, LivingEntity target)
    {
        Ability summonUndead = caster.mySpellBook.GetAbilityByName("Summon Undead");
        OnAbilityUsed(summonUndead, caster);
        List<TileScript> allPossibleSpawnLocations = LevelManager.Instance.GetValidMoveableTilesWithinRange(summonUndead.abilityRange, caster.TileCurrentlyOn);
        List<TileScript> finalList = new List<TileScript>();

        // if target is to the left
        if (target.GridPosition.X <= caster.GridPosition.X)
        {
            foreach (TileScript tile in allPossibleSpawnLocations)
            {
                if (tile.GridPosition.X >= target.GridPosition.X && tile.GridPosition.X <= caster.GridPosition.X)
                {
                    finalList.Add(tile);
                }
            }
        }

        // if target is to the right
        else if (target.GridPosition.X > caster.GridPosition.X)
        {
            foreach (TileScript tile in allPossibleSpawnLocations)
            {
                if (tile.GridPosition.X <= target.GridPosition.X && tile.GridPosition.X >= caster.GridPosition.X)
                {
                    finalList.Add(tile);
                }
            }
        }

        // summon skeletons loop
        for (int skeletonsSummoned = 0; skeletonsSummoned < summonUndead.abilityPrimaryValue; skeletonsSummoned++)
        {
            TileScript spawnLocation = LevelManager.Instance.GetClosestValidTile(finalList, target.TileCurrentlyOn);

            GameObject newSkeletonGO = Instantiate(PrefabHolder.Instance.ZombiePrefab);
            Enemy newSkeleton = newSkeletonGO.GetComponent<Enemy>();

            newSkeleton.InitializeSetup(spawnLocation.GridPosition, spawnLocation);
        }

        

        yield return null;
    }

    // Healing Light
    public void PerformHealingLight(LivingEntity caster, LivingEntity target)
    {
        StartCoroutine(PerformHealingLightCoroutine(caster, target));
    }
    public IEnumerator PerformHealingLightCoroutine(LivingEntity caster, LivingEntity target)
    {
        Ability healingLight = caster.mySpellBook.GetAbilityByName("Healing Light");
        OnAbilityUsed(healingLight, caster);
        target.ModifyCurrentHealth(healingLight.abilityPrimaryValue);        

        yield return null;
    }

}
