using System;
using UnityEngine;

public class QuestionNode : ITreeNode
{
    Func<bool> question;
    ITreeNode tNode;
    ITreeNode fNode;

    public QuestionNode(Func<bool> question, ITreeNode tNode, ITreeNode fNode)
    {
        this.question = question;
        this.tNode = tNode;
        this.fNode = fNode;
    }

    public void Execute()
    {
       //question.Invoke() ?? tNode.Execute() :: fNode.Execute(); 
       if(question.Invoke())
            tNode.Execute();
       else
            fNode.Execute();
    }
}
