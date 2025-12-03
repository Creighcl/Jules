using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StageChoreographer : MonoBehaviour
{
    EventProvider _eventProvider;
    List<string> PerformancesInFlight = new List<string>();
    [SerializeField]
    bool PerforminRN = false;
    void Update() {
        PerforminRN = IsPerforming();
    }
    public bool IsPerforming() {
        int MyPerformanceCount = PerformancesInFlight.Count;
            int MyActorsPerformingCount = MyActors.Count(actor => actor.IsPerforming);
        return MyPerformanceCount > 0 || MyActorsPerformingCount > 0;
    }
    List<ActorCharacter> MyActors = new List<ActorCharacter>();

    void Awake() {
        _eventProvider = GetComponent<CombatReferee>().eventProvider;
        SetupHooks();
    }

    void SetupHooks() {
        _eventProvider.OnPhasePrompt += HandlePhasePrompts;
        // _eventProvider.OnWaveReady += HandleWaveReady;
        _eventProvider.OnEffectPlanExecutionComplete += HandleAbilityExecuted;
        _eventProvider.OnCharacterRevived += HandleCharacterRevived;
        _eventProvider.OnDamageResolved += HandleDamageResolved;
        _eventProvider.OnEffectPlanExecutionStart += HandleEffectPlanExecutionStart;
        _eventProvider.OnBuffAdded += HandleBuffAdded;
        _eventProvider.OnBuffExpired += HandleBuffRemoved;
        // _eventProvider.OnStageComplete += HandleStageComplete;
        _eventProvider.OnCharacterSummoned += HandleCharacterSummoned;
    }

    ActorCharacter GetActor(Character character) {
        if (character == null || character.ViewRef == null) return null;
        var behavior = character.ViewRef as CharacterBehavior;
        if (behavior == null) return null;
        return behavior.GetComponent<ActorCharacter>();
    }

    void HandleCharacterSummoned(Character character) {
        var actor = GetActor(character);
        if (actor != null) MyActors.Add(actor);
    }

    // void HandleStageComplete() {
        // MyActors
        //     .Where(actor => actor._character.isDead && actor._character.Config.TeamType == TeamType.PLAYER)
        //     .ToList()
        //     .ForEach(actor => {
        //     if (actor.GetComponent<Character>().isDead) {
        //         actor.EnqueuePerformance(CharacterActorPerformance.FADEOUT);
        //     }
        // });
    // }

    void PolymorphCharacter(Character character, bool isPolymorphed) {
        ActorCharacter actor = GetActor(character);
        if (actor == null) return;

        if (isPolymorphed) {
            actor.EnqueuePerformance(CharacterActorPerformance.POLYMORPH);
        } else {
            actor.EnqueuePerformance(CharacterActorPerformance.UNPOLYMORPH);
        }
    }

    void HandleBuffAdded(Buff buff) {
        if (buff is BuffPolymorph) {
            PolymorphCharacter(buff.Target, true);
        }
        GetActor(buff.Target)?.FloatingBuffDown(buff);
    }

    void HandleBuffRemoved(Buff buff) {
        if (buff is BuffPolymorph) {
            PolymorphCharacter(buff.Target, false);
        }
        GetActor(buff.Target)?.FloatingBuffUp(buff);
    }

    // void HandleWaveReady(int waveNum) {

    // }

    IEnumerator WaitPerformance(float duration, string name) {
        PerformancesInFlight.Add(name);
        yield return new WaitForSeconds(duration);
        PerformancesInFlight.Remove(name);
    }

    void HandlePhasePrompts(CombatPhase phase, Character combatant) {
        if (phase == CombatPhase.CHARACTERTURN_EXECUTION) {
            HandleExecutionPhasePromptForCharacter(combatant);
        }
    }

    void HandleExecutionPhasePromptForCharacter(Character combatant) {
        // StartCoroutine(WaitPerformance(10f, "executionlull"));
    }

    void HandleEffectPlanExecutionStart(EffectPlan plan) {
        var actor = GetActor(plan.Caster);
        if (actor == null) return;

        if (plan.Source is AbilityBasicAttack) {
            actor.EnqueuePerformance(CharacterActorPerformance.BASICATTACK);
        } else {
            actor.EnqueuePerformance(CharacterActorPerformance.SPECIALATTACK);
        }

        if (plan.Source is AbilityHollowHowl) {
            WaitPerformance(4f, "howling");
        }
    }

    void HandleDamageResolved(CalculatedDamage cd) {
        // ActorCharacter sourceMotor = cd.Attacker.GetComponent<ActorCharacter>();
        // if (cd.Source is AbilityBasicAttack) {
        //     sourceMotor.EnqueuePerformance(CharacterActorPerformance.BASICATTACK);
        // } else {
        //     sourceMotor.EnqueuePerformance(CharacterActorPerformance.SPECIALATTACK);
        // }
        ActorCharacter victimMotor = GetActor(cd.Target);
        if (victimMotor == null) return;

        victimMotor.FloatingDamageText(cd.DamageToHealth);

        if (cd.DamageToHealth > 0) {

            victimMotor.EnqueuePerformance(CharacterActorPerformance.TAKEDAMAGE);
            if (cd.Target.isDead) {
                victimMotor.EnqueuePerformance(CharacterActorPerformance.DIE);
            } else if (cd.StaggerCrackedByThis) {
                victimMotor.EnqueuePerformance(CharacterActorPerformance.CRACKED);
            }
        }
    }

    void HandleAbilityExecuted(EffectPlan executedAbility) {
        // after all things have resolved
    }

    void HandleCharacterRevived(Character character) {
        GetActor(character)?.EnqueuePerformance(CharacterActorPerformance.REVIVE);
    }
}
