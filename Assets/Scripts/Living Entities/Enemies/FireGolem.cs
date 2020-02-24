﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireGolem : Enemy
{
    public override void SetBaseProperties()
    {
        myName = "Fire Golem";
        base.SetBaseProperties();

        CharacterModelController.SetUpAsFireGolemPreset(myModel);

        mySpellBook.EnemyLearnAbility("Move");
        mySpellBook.EnemyLearnAbility("Strike");
        mySpellBook.EnemyLearnAbility("Fire Ball");
        mySpellBook.EnemyLearnAbility("Fire Nova");

        myPassiveManager.ModifyFieryAura(3);

        myMainHandWeapon = ItemLibrary.Instance.GetItemByName("Simple Staff");
    }

    public override IEnumerator StartMyActivationCoroutine()
    {
        Ability fireBall = mySpellBook.GetAbilityByName("Fire Ball");
        Ability move = mySpellBook.GetAbilityByName("Move");
        Ability strike = mySpellBook.GetAbilityByName("Strike");
        Ability fireNova = mySpellBook.GetAbilityByName("Fire Nova");


        ActionStart:

        SetTargetDefender(EntityLogic.GetBestTarget(this, true));

        while (EventManager.Instance.gameOverEventStarted)
        {
            yield return null;
        }

        if (EntityLogic.IsAbleToTakeActions(this) == false)
        {
            EndMyActivation();
        }

        // Fire Nova
        else if (EntityLogic.GetAllEnemiesWithinRange(this, 1).Count > 1 &&
            EntityLogic.IsAbilityUseable(this, fireNova))
        {
            VisualEffectManager.Instance.CreateStatusEffect(transform.position, "Fire Nova");
            yield return new WaitForSeconds(0.5f);

            Action action = AbilityLogic.Instance.PerformFireNova(this);
            yield return new WaitUntil(() => action.ActionResolved() == true);

            // brief delay between actions
            yield return new WaitForSeconds(1f);
            goto ActionStart;
        }

        // Fireball 
        else if (EntityLogic.IsTargetInRange(this, myCurrentTarget, fireBall.abilityRange) &&
            EntityLogic.IsAbilityUseable(this, fireBall))
        {
            VisualEffectManager.Instance.CreateStatusEffect(transform.position, "Fire Ball");
            yield return new WaitForSeconds(0.5f);

            Action action = AbilityLogic.Instance.PerformFireBall(this, myCurrentTarget);
            yield return new WaitUntil(() => action.ActionResolved() == true);

            yield return new WaitForSeconds(1f);
            goto ActionStart;
        }        

        // Move
        else if (
            EntityLogic.IsTargetInRange(this, myCurrentTarget, fireBall.abilityRange) == false &&
            EntityLogic.IsAbleToMove(this) &&
            EntityLogic.IsAbilityUseable(this, move) &&
            EntityLogic.CanPerformAbilityTwoAfterAbilityOne(move, fireBall, this) &&
            EntityLogic.GetBestValidMoveLocationBetweenMeAndTarget(this, myCurrentTarget, currentMeleeRange, EntityLogic.GetTotalMobility(this)) != null
            )
        {
            VisualEffectManager.Instance.CreateStatusEffect(transform.position, "Move");
            yield return new WaitForSeconds(0.5f);

            Tile destination = EntityLogic.GetBestValidMoveLocationBetweenMeAndTarget(this, myCurrentTarget, currentMeleeRange, EntityLogic.GetTotalMobility(this));
            Action movementAction = AbilityLogic.Instance.PerformMove(this, destination);
            yield return new WaitUntil(() => movementAction.ActionResolved() == true);

            // small delay here in order to seperate the two actions a bit.
            yield return new WaitForSeconds(1f);
            goto ActionStart;
        }

        // Can't do anything more, end activation
        else
        {
            EndMyActivation();
        }
    }
}
