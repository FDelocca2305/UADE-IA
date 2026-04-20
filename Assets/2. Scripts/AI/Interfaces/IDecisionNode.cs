/// <summary>
/// Base interface for decision tree nodes used in AI behavior systems.
/// Allows for modular and reusable AI decision making through composition.
/// 
/// MEJORA: Agregado soporte para prioridades para mejor control de decisiones
/// MEJORA: Agregado IsReusable para optimización de nodos frecuentemente utilizados
/// </summary>
public interface IDecisionNode
{
    /// <summary>
    /// Evaluates this decision node and returns the next node to execute
    /// or null if this is a leaf action node
    /// </summary>
    /// <param name="context">The AI context containing all necessary information</param>
    /// <returns>Next node to evaluate or null for action execution</returns>
    IDecisionNode Evaluate(IAIContext context);
    
    /// <summary>
    /// Gets the display name of this node for debugging purposes
    /// </summary>
    string GetNodeName();
    
    /// <summary>
    /// MEJORA: Prioridad del nodo para resolver conflictos entre múltiples opciones válidas
    /// Valores más altos = mayor prioridad
    /// </summary>
    int GetPriority() => 0;
    
    /// <summary>
    /// MEJORA: Indica si este nodo puede ser reutilizado sin recreación
    /// Útil para optimización de nodos stateless frecuentemente utilizados
    /// </summary>
    bool IsReusable => true;
    
    /// <summary>
    /// MEJORA: Método opcional para reset de estado interno
    /// Llamado cuando el nodo es reutilizado
    /// </summary>
    void Reset() { }
}