import { IssueEstimatePredictionFactorModel } from "./issueEstimatePredictionFactorModel";

export class IssueEstimatePredictionModel {
    estimateSeconds!: number;
    estimate!: string;
    usedMlModel!: boolean;
    trainingSamples!: number;
    confidence!: number;
    factors: IssueEstimatePredictionFactorModel[] = [];
}
