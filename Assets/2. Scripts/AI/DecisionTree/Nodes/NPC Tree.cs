#if SAMPLE_DT
using System;
using UnityEngine;

public class NPCTree : MonoBehaviour
{
    [SerializeField] int health;
    [SerializeField] int maxHealth;
    [SerializeField] FOV fieldOfView;
    [SerializeField] GameObject target;
    [SerializeField] Transform[] waypoints;
    [SerializeField] int currentWP;
    [SerializeField] float attackRange;
    [SerializeField] float speed;
    [SerializeField, Range(0, 100)] int lowHealthThreshold;
    private ITreeNode _rootNode;

    public delegate void MiDelegado();
    public MiDelegado _miDelegado;

    public Action miDelegadoAction;
    public Action<string> callbackWMessage;

    public Func<string> callbackSendMsg;
    public Func<int, string> printsNumber;

    void Start()
    {
        //fieldOfView.Target = target;
        //
        //_miDelegado = Die;
        //miDelegadoAction = Patrol;
        //callbackWMessage = HandleMessage;

        CreateTree();
    }

    private void CreateTree()
    {
        ActionNode die = new(Die);
        ActionNode patrol = new(Patrol);
        ActionNode idle = new(Idle);
        ActionNode flee = new(Flee);
        ActionNode persuit = new(Persuit);
        ActionNode attack = new(Attack);


        QuestionNode inAttackRange = new(() => 
            Vector3.Distance(transform.position, target.transform.position) < attackRange, attack, persuit);
        QuestionNode arrivedAtPoint = new(() =>
            Vector3.Distance(transform.position, waypoints[currentWP].position) < 0.3f, idle, patrol);
        QuestionNode lowHealth = new(() => health < maxHealth * lowHealthThreshold / 100, flee, inAttackRange);
        QuestionNode isPInSight = new(fieldOfView.CheckDetection, lowHealth, arrivedAtPoint);
        QuestionNode isAlive = new(() => health <= 0, die, isPInSight);

        _rootNode = isAlive;
    }

    
    private bool IsAlive()
    {
        return health <= 0;
    }
    // Update is called once per frame
    void Update()
    {
        /*if (health <= 0)
            Die();
        else
        {
            if (fieldOfView.CheckDetection())
            {
                if (health <= maxHealth * lowHealthThreshold / 100)
                    Flee();
                else
                {
                    if(Vector3.Distance(transform.position, target.transform.position) < attackRange)
                        Attack();
                    else
                        Persuit();
                }
            }
            else
            {
                if(Vector3.Distance(transform.position, waypoints[currentWP].position) < 0.3f)
                    Idle();
                else
                    Patrol();
            }
        }*/
        _rootNode.Execute();
    }

    private void HandleMessage(string message) { }
    private void Die() { MyLogger.LogInfo("Die"); }
    private void Flee() { MyLogger.LogInfo("Flee"); }
    private void Attack() { MyLogger.LogInfo("Attack"); }
    private void Patrol() { MyLogger.LogInfo("Patrol"); }
    private void Idle() { MyLogger.LogInfo("Idle"); }
    private void Persuit() { MyLogger.LogInfo("Persuit"); }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
#endif

