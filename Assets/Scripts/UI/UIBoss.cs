using UnityEngine;
using System.Collections;

public class UIBoss : MonoBehaviour {
    public const string takeEnter = "enter";
    public const string takeExit = "exit";

    private UISlider mSlider;
    private AnimatorData mAnim;

    public bool isAnimPlaying { get { return mAnim.isPlaying; } }

    public void Enter() {
        gameObject.SetActive(true);
        mAnim.Play(takeEnter);
    }

    public void Exit() {
        if(gameObject.activeSelf)
            mAnim.Play(takeExit);
    }

    public void Init(Enemy enemy) {
        enemy.stats.changeHPCallback += OnEnemyStatHPChange;

        enemy.setStateCallback += OnEnemyChangeState;
    }


    void Awake() {
        mSlider = GetComponent<UISlider>();
        mAnim = GetComponent<AnimatorData>();

        gameObject.SetActive(false);
    }

    void OnEnemyStatHPChange(Stats s, float delta) {
        mSlider.value = s.curHP/s.maxHP;
    }

    void OnEnemyChangeState(EntityBase ent) {
        if(ent.state == (int)EntityState.Dead) {
            Exit();
        }
    }
}
