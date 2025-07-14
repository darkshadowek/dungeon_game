using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SlimeScript : MonsterScript
{
    private Animator animator;
    private AnimatorStateInfo stateInfo;
    private bool isJumping = false;
    private Vector3 jumpDirection;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        animator = GetComponent<Animator>();
        if (IsServer)
        {
            StartCoroutine(Move());
        }
    }

    private void Update()
    {
        stateInfo = animator.GetCurrentAnimatorStateInfo(0);

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
            // Zatrzymaj ruch gdy animacja si� sko�czy
            if (isJumping)
            {
                isJumping = false;
            }
        }
    }

    private void StartJump()
    {
        isJumping = true;
        // Wygeneruj ca�kowicie losowy kierunek
        float angle = Random.Range(0f, Mathf.PI * 2f);
        jumpDirection = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0).normalized;
    }

    IEnumerator Move()
    {
        while (true)
        {
            // Uruchom animacj� skoku
            animator.SetTrigger("Jump"); // Za��my �e masz trigger "Jump"

            // Czekaj na zako�czenie animacji
            yield return new WaitForSeconds(1f); // Dostosuj do d�ugo�ci animacji

            float waitTime = Random.Range(2f, 7f);
            yield return new WaitForSeconds(waitTime);
        }
    }
}