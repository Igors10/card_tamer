using FishNet.Demo.AdditiveScenes;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DamageOnPlay : MonoBehaviour
{
    private void Start()
    {
        // adding this to effect stack on play
        GameManager.instance.executeManager.effectStack.Add(UseEffect());
    }
    IEnumerator UseEffect()
    {
        Effect effect = GetComponent<Effect>();
        yield return StartCoroutine(effect.ShowEffect());

        // DEALING DAMAGE
        // ==============
        // getting variables
        GameObject dmgParticlePrefab = GameManager.instance.battleManager.dmgParticlePrefab; ;
        GameObject damageObj = GameManager.instance.battleManager.damageObj;
        float damageParticleOffset = 40f;

        // spawning damage particle
        Vector3 unitPos = Camera.main.WorldToScreenPoint(effect.unit.transform.position);
        GameObject newDamageParticle = Instantiate(dmgParticlePrefab, unitPos, Quaternion.identity, damageObj.transform);
        newDamageParticle.GetComponent<Image>().color = effect.unit.card.player.playerColor;
        newDamageParticle.transform.localPosition += new Vector3(damageParticleOffset, 0f, 0f);

        // playing soundeffect
        AudioManager.instance.PlaySFX("MoreDamageSFX");

        // pop animation
        GameManager.instance.animations.PopAnim(newDamageParticle, 0.3f, 0.45f);
        yield return new WaitForSeconds(0.2f);

        // sending damage particle
        Player playerToDamage = GameManager.instance.GetOpponentOfPlayer(effect.unit.card.player);
        Vector3 healthbarPos = playerToDamage.playerUI.healthbar.transform.position;
        effect.unit.card.player.powerCounter.gameObject.SetActive(true);
        effect.unit.card.player.powerCounter.playerCounterVisuals.SetActive(false);
        yield return effect.unit.card.player.powerCounter.Damage(healthbarPos, newDamageParticle);
        effect.unit.card.player.powerCounter.gameObject.SetActive(false);
        effect.unit.card.player.powerCounter.playerCounterVisuals.SetActive(true);

        playerToDamage.TakeDamage(1);

        yield return StartCoroutine(effect.StopShowEffect());
    }
}
