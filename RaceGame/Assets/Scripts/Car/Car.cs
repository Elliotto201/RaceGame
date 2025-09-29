using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

public class Car : NetworkBehaviour
{
    private PrometeoCarController CarController;
    private CarPrediction CarInput;
    private CarAbilities CarAbilities;
    //Server Side only abilities
    private Ability m_Ability1;
    private Ability m_Ability2;
    private Ability m_Ability3;
    private Ability m_Ability4;

    private Dictionary<string, MethodInfo> cachedAbilityMethods = new();

    private NetworkVariable<int> currentHealth;
    public int CurrentHealth 
    {
        get
        {
            return currentHealth.Value;
        }
    }

    [Header("Misc")]
    [SerializeField] private GameObject PlayerCam;
    [SerializeField] private Press LeftButton, RightButton;
    [SerializeField] private AbilityList AbilityList;

    [Header("Health")]
    [SerializeField] private int MaxHealth;
    [SerializeField] private Slider HealthSlider;
    [Header("Ability Controls")]
    [SerializeField] private Button Ability1;
    [SerializeField] private Button Ability2;
    [SerializeField] private Button Ability3;
    [SerializeField] private Button Ability4;

    private void Awake()
    {
        CarInput = GetComponent<CarPrediction>();
        CarController = GetComponent<PrometeoCarController>();
        CarAbilities = GetComponent<CarAbilities>();

        cachedAbilityMethods.Clear();

        currentHealth = new NetworkVariable<int>(MaxHealth, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        HealthSlider.minValue = 0;
        HealthSlider.maxValue = MaxHealth;
        HealthSlider.value = MaxHealth;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            var go = Instantiate(PlayerCam, PlayerCam.transform.position, PlayerCam.transform.rotation);
            go.GetComponent<CameraFollow>().carTransform = transform;

            var cf = go.GetComponent<CameraFollow>();

            Ability1.onClick.AddListener(_Ability1);
            Ability2.onClick.AddListener(_Ability2);
            Ability3.onClick.AddListener(_Ability3);
            Ability4.onClick.AddListener(_Ability4);
        }

        if (IsClient)
        {
            currentHealth.OnValueChanged += HealthChangedClients;
        }
    }   

    public void SetAbilities(Ability ability1, Ability ability2, Ability ability3, Ability ability4)
    {
        m_Ability1 = ability1;
        m_Ability2 = ability2;
        m_Ability3 = ability3;
        m_Ability4 = ability4;

        ChangeStatsFromAbility(ability1);
        ChangeStatsFromAbility(ability2);
        ChangeStatsFromAbility(ability3);
        ChangeStatsFromAbility(ability4);

        var abilities = new FixedString32Bytes[4];

        abilities[0] = ability1.Name;
        abilities[1] = ability2.Name;
        abilities[2] = ability3.Name;
        abilities[3] = ability4.Name;

        SetIconAbilitiesOwnerRpc(abilities);
    }

    private void ChangeStatsFromAbility(Ability ability)
    {
        foreach(var statMod in ability.StatModifications)
        {
            float multiplier = statMod.StatChange / 100f;

            if (statMod.Type == StatType.Health)
            {
                currentHealth.Value = (int)(MaxHealth * multiplier);
            }
            else if(statMod.Type == StatType.Speed)
            {
                int currentSpeed = CarController.maxSpeed;
                int multipliedSpeed = (int)(currentSpeed * multiplier);

                CarController.maxSpeed = multipliedSpeed;
                SetMoveSpeedOnClientRpc(multipliedSpeed);
            }
        }
    }

    [Rpc(SendTo.Owner)]
    private void SetIconAbilitiesOwnerRpc(FixedString32Bytes[] abilityIcons)
    {
        for(int i = 0; i < 4; i++)
        {
            var abilityIcon = AbilityList.Abilities.First(t => t.Name == abilityIcons[i]).Icon;

            if (abilityIcon != null)
            {
                if(i == 0)
                {
                    SetIconOfAbility(Ability1.transform, abilityIcon);
                }
                else if(i == 1)
                {
                    SetIconOfAbility(Ability2.transform, abilityIcon);
                }
                else if(i == 2)
                {
                    SetIconOfAbility(Ability3.transform, abilityIcon);
                }
                else if(i == 3)
                {
                    SetIconOfAbility(Ability4.transform, abilityIcon);
                }
            }
        }
    }

    private void SetIconOfAbility(Transform buttonTransform, Sprite sprite)
    {
        buttonTransform.GetChild(0).GetComponent<Image>().sprite = sprite;
    }

    [Rpc(SendTo.Owner)]
    private void SetMoveSpeedOnClientRpc(int speed)
    {
        CarController.maxSpeed = speed;
    }

    private void _Ability1()
    {
        TryUseAbilityServerRpc(1);
    }
    private void _Ability2()
    {
        TryUseAbilityServerRpc(2);
    }
    private void _Ability3()
    {
        TryUseAbilityServerRpc(3);
    }
    private void _Ability4()
    {
        TryUseAbilityServerRpc(4);
    }

    [Rpc(SendTo.Server)]
    private void TryUseAbilityServerRpc(byte ability)
    {
        if(ability == 1)
        {
            UseAbility(m_Ability1);
        }
        else if(ability == 2)
        {
            UseAbility(m_Ability2);
        }
        else if(ability == 3)
        {
            UseAbility(m_Ability3);
        }
        else if(ability == 4)
        {
            UseAbility(m_Ability4);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        bool steerLeft = false;
        bool steerRight = false;
        bool steerBackwards = false;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (Input.GetKey(KeyCode.A))
        {
            steerLeft = true;
        }
        if (Input.GetKey(KeyCode.D))
        {
            steerRight = true;
        }
        if (Input.GetKey(KeyCode.S))
        {
            steerBackwards = true;
        }
#else
         if (LeftButton.isPressed)
        {
            steerLeft = true;
        }
        if (RightButton.isPressed)
        {
            steerRight = true;
        }
        if(RightButton.isPressed && LeftButton.isPressed)
        {
            steerBackwards = true;
        }
#endif

        CarInput.SetInput(new CarInput
        {
            SteerBack = steerBackwards,
            SteerLeft = steerLeft,
            SteerRight = steerRight
        });
    }

    private void HealthChangedClients(int previousValue, int newValue)
    {
        HealthSlider.value = newValue;
        Debug.Log("Health Value Changed on client to: " + newValue);
    }

    [Preserve]
    private void UseAbility(Ability ability)
    {
        if (!IsServer) return;
        if (ability.AbilityMethodName.Length == 0) return;

        MethodInfo method;
        if (cachedAbilityMethods.TryGetValue(ability.AbilityMethodName, out MethodInfo info))
        {
            method = info;
        }
        else
        {
            method = typeof(CarAbilities).GetMethod(ability.AbilityMethodName, BindingFlags.Public | BindingFlags.Instance);
        }

        if (method != null)
        {
            method.Invoke(CarAbilities, null);
        }
        else
        {
            Debug.LogWarning($"Abilties non 0 length method name does not exist {ability.AbilityMethodName}");
        }
    }

    public void SetHealth(int health)
    {
        if (!IsServer) return;

        currentHealth.Value = Mathf.Clamp(health, 0, MaxHealth);
        if (currentHealth.Value == 0)
        {
            Debug.Log("Car dead");
        }
    }

    public void Damage(int damage)
    {
        if(!IsServer) return;

        int a = currentHealth.Value - damage;
        currentHealth.Value = Mathf.Clamp(a, 0, MaxHealth);

        if(currentHealth.Value == 0)
        {
            Debug.Log("Car dead");
        }
    }

    public void Heal(int heal)
    {
        if (!IsServer) return;

        currentHealth.Value += heal;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value, 0, MaxHealth);
    }
}
