using System.Collections;
using UnityEngine;

[System.Serializable]
public class Buff
{
    public StatType type;
    public float value;
}

//玩家获得的Buff
public class Object_Buff : MonoBehaviour
{
    private SpriteRenderer sr;
    private Entity_Stats statsToModify;

    [Header("Buff details")]
    [SerializeField] private Buff[] buffs;
    [SerializeField] private float buffDuration = 4;
    [SerializeField] private bool canBeUsed = true; //是否可被触发；生效中设为 false 防止重复碰撞
    [SerializeField] private string buffName;


    [Header("Floaty movement")] //上下漂浮的效果
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float floatRange = 0.1f;
    private Vector3 startPosition;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        startPosition = transform.position;
    }

    private void Update()
    {
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatRange;
        transform.position = startPosition + new Vector3(0, yOffset);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (canBeUsed == false) return;

        statsToModify = collision.GetComponent<Entity_Stats>();
        StartCoroutine(BuffCo(buffDuration));
    }

    private IEnumerator BuffCo(float duration)
    {
        //Buff生效中,GameObject透明化
        canBeUsed = false;
        sr.color = Color.clear;
        foreach(var buff in buffs)
        {
            statsToModify.GetStatByType(buff.type).AddModifier(buff.value, buffName);
        }

        yield return new WaitForSeconds(duration);

        foreach(var buff in buffs)
        {
            statsToModify.GetStatByType(buff.type).RemoveModifier(buffName);
        }
        Destroy(gameObject);
    }
}
