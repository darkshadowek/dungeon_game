using System.Collections;
using UnityEngine;

public class SlimeScript : MonsterScript
{
    private Animator animator;
    private AnimatorStateInfo stateInfo;
    [SerializeField] private float timeTojump;
    [SerializeField] private float detectDistance;
    [SerializeField] private float attackDistance;
    private bool isJumping = false;
    private Vector3 jumpDirection = Vector3.zero;
    public bool isAttacking;
    private GameObject player;
    private IAttackable playerInterface;
    void Start()
    {
        health = maxHealth;
        animator = GetComponent<Animator>();
        StartCoroutine(Move());
        player = GameObject.FindGameObjectWithTag("Player");
        playerInterface = player.GetComponent<IAttackable>();
    }

    private void Update()
    {
        HealthVisualisation();
        stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // Logika ruchu
        if (stateInfo.IsName("Slime_Jump"))
        {
            if (!isJumping)
            {
                // Rozpocznij ruch w losowym kierunku
                StartJump();
            }

            if (isJumping)
            {
                transform.position += jumpDirection * speed * Time.deltaTime;
            }
        }
        else
        {
            // Zatrzymaj ruch gdy animacja siê skoñczy
            if (isJumping)
            {
                isJumping = false;
            }
        }
    }

    private void StartJump()
    {
        isJumping = true;
        bool isEnemyHere = DetectPlayer(detectDistance, attackDistance);
        if (canAttackPlayer && isEnemyHere)
        {
            Vector3 direction = player.transform.position - transform.position;
            jumpDirection = direction.normalized;

        }
        else
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            jumpDirection = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0).normalized;
        }
    }
    private bool DetectPlayer(float distanceToWalk, float distacneToAttack)
    {
        float distanceBetween = Vector3.Distance(transform.position, player.transform.position);
        if (distanceBetween < distacneToAttack)
        {
            isAttacking = true;
            return true;
        }
        else if (distanceBetween < distanceToWalk)
        {
            isAttacking = false;
            return true;
        }
        else
        {
            isAttacking = false;
            return false;
        }
    }
    IEnumerator Move()
    {
        while (true)
        {
            animator.SetTrigger("Jump");


            yield return new WaitForSeconds(1f);
            if (isAttacking) Attack();

            float waitTime = Random.Range(2f, timeTojump);
            yield return new WaitForSeconds(waitTime);

        }
    }

    private void Attack()
    {
        float distanceToAttack = Vector3.Distance(transform.position, player.transform.position);
        if(distanceToAttack < attackDistance)
        {
            playerInterface.TakeDamage(damage);
        }
        else
        {
            print("gracz za daleko");
        }
    }
}