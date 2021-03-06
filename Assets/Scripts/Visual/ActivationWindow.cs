﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ActivationWindow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Component References")]
    public TextMeshProUGUI rollText;
    public Slider myHealthBar;
    public GameObject myGlowOutline;
    public CanvasGroup myCanvasGroup;
    public UniversalCharacterModel myUCM;

    [Header("Properties")]
    public LivingEntity myLivingEntity;
    public bool animateNumberText;
    public bool dontFollowSlot;
    public bool update;
    public void InitializeSetup(LivingEntity entity)
    {
        myLivingEntity = entity;
        entity.myActivationWindow = this;
        gameObject.SetActive(false);
        gameObject.SetActive(true);

        Debug.Log("Setting up activation window UC Model for " + entity.myName);

        // Set up model   
        
        if (entity.defender)
        {
            CharacterModelController.BuildModelFromModelClone(myUCM, entity.defender.myCharacterData.myCharacterModel);
        }
        else if (entity.enemy)
        {
            //CharacterModelController.BuildModelFromPresetString(myUCM, entity.myName);
            CharacterModelController.BuildModelFromModelClone(myUCM, entity.myModel);
        }
        
        myUCM.SetBaseAnim();
    }

    private void Update()
    {
        int myCurrentActivationOrderIndex = 0;

        if (ActivationManager.Instance.activationOrder.Count > 0 
            && ActivationManager.Instance.updateWindowPositions == true)
        {
            // TO DO: 2 lines below prevent buggy behabiour in unity that prevents the activation window's health bar
            // from rendering correctly. Doing this in update degrades performance. fix in the future

            //ActivationManager.Instance.activationPanelParentCanvas.overrideSorting = true;
           // ActivationManager.Instance.activationPanelParentCanvas.sortingOrder = 19;

            for (int i = 0; i < ActivationManager.Instance.activationOrder.Count; i++)
            {
                // Check if GameObject is in the List
                if (ActivationManager.Instance.activationOrder[i] == myLivingEntity)
                {
                    // It is. Return the current index
                    myCurrentActivationOrderIndex = i;
                    break;
                }
            }

            if (ActivationManager.Instance.panelSlots != null &&
                ActivationManager.Instance.panelSlots.Count -1 >= myCurrentActivationOrderIndex &&
                ActivationManager.Instance.panelSlots[myCurrentActivationOrderIndex] != null &&
                transform.position != ActivationManager.Instance.panelSlots[myCurrentActivationOrderIndex].transform.position)
            {
                MoveTowardsSlotPosition(ActivationManager.Instance.panelSlots[myCurrentActivationOrderIndex]);
            }
        }    
        
        
    }
    public Action FadeOutWindow()
    {
        Action action = new Action();
        StartCoroutine(FadeOutWindowCoroutine(action));
        return action;
    }
    public IEnumerator FadeOutWindowCoroutine(Action action)
    {
        while (myCanvasGroup.alpha > 0)
        {
            myCanvasGroup.alpha -= 0.05f;
            if (myCanvasGroup.alpha == 0)
            {
                GameObject slotDestroyed = ActivationManager.Instance.panelSlots[ActivationManager.Instance.panelSlots.Count - 1];
                if (ActivationManager.Instance.activationOrder.Contains(myLivingEntity))
                {
                    ActivationManager.Instance.activationOrder.Remove(myLivingEntity);
                }                
                ActivationManager.Instance.panelSlots.Remove(slotDestroyed);                
                Destroy(slotDestroyed);
            }
            yield return new WaitForEndOfFrame();
        }

        action.actionResolved = true;
        Destroy(gameObject);
    }

    public void DestroyWindowOnCombatEnd()
    {
        Destroy(gameObject);
    }
    public void MoveTowardsSlotPosition(GameObject slot)
    {
        if (!dontFollowSlot)
        {
            transform.position = Vector2.MoveTowards(transform.position, slot.transform.position, 10 * Time.deltaTime);
        }
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("ActivationWindow.OnPointerClick() called...");
        // Clicking on a character's activation window performs the same logic as clicking on the character itself
        if (myLivingEntity.defender)
        {
            myLivingEntity.defender.OnMouseDown();
        }
        else if (myLivingEntity.enemy)
        {
            myLivingEntity.enemy.OnMouseDown();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("ActivationWindow.OnPointerEnter() called...");
        ActivationManager.Instance.panelIsMousedOver = true;
        myGlowOutline.SetActive(true);
        if (myLivingEntity != null)
        {
            myLivingEntity.SetColor(myLivingEntity.highlightColour);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("ActivationWindow.OnMouseEnter called...");
        ActivationManager.Instance.panelIsMousedOver = false;
        myGlowOutline.SetActive(false);
        if (myLivingEntity != null)
        {
            myLivingEntity.SetColor(myLivingEntity.normalColour);
        }
    }    
   
}
