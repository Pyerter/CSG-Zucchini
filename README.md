# CSG-Zucchini
This is a small game that will be worked on over the Summer in 2021.<br/>
Collaborators: Porter Squires, Michael Zabrowski, Alex Zabrowski, and Nicholas Pautsch.

##### Table of Contents
1. [Work Flow](#Work-Flow)
1. [Work Environments](#Work-Environments)
1. [Game Overview](https://github.com/Pyerter/CSG-Zucchini/blob/master/_Storyboarding/Game%20Overview.md)


### Work Flow

Our Master Branch will act as the release branch and the structure will follow like so:
```
Master
   |  \__ Developer
   |          |    \__ Feature
   |          |           |   \__ Feature Patch
   |          |           |             |
   |          |           |             |
   |          |           |             |
Master    Developer    Feature    Feature Patch
   |          |           |        /
   |          |        Feature __ /
   |      Developer __ /
Master __ /
```
Whenever we merge a Feature branch into the Developer branch, we will use pull requests so that at least one other collaborator can check for merge conflicts. <br/>
Feature Patches will be small enough there there should not be any merge conflicts. The benefit of using Feature Patch branches will allow more than one person to work on a Feature at the same time.<br/>
When we complete a Feature and merged it into Developer in a working state, we will delete the corresponding Feature branch. If we are to work on that Feature again, we will make another branch for that Feature with the same name.<br/>

Feature Naming Conventions:
Ftr-feature-name

Feature Patch Naming Conventions:
Ftr-feature-name-Ptch-patch-name

**REMINDER**: When you start working on this project at any point, please make sure that you have pulled the most recent commits and are working in the correct branch. If you fail to do this, it could result in merge conflicts that we do not want.


### Work Environments

For building the game, we will be using the latest version of unity, currently: 2021.1.5f1.



