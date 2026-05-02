import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { SurveysApiService } from '../../services/surveys-api.service';
import { SurveyComprehensiveAnalyticsDto, QuestionAnalyticsDto, AnswerDistributionDto } from '../../shared/models/questionnaire.models';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { NgIf, NgFor, DecimalPipe } from '@angular/common';
import { BaseChartDirective } from 'ng2-charts';
import { Chart, ChartData, ChartOptions, registerables } from 'chart.js';

@Component({
  selector: 'app-survey-question-analytics-page',
  standalone: true,
  imports: [RouterLink, TranslatePipe, NgIf, NgFor, DecimalPipe, BaseChartDirective],
  templateUrl: './survey-question-analytics-page.component.html',
  styleUrl: './survey-question-analytics-page.component.scss',
})
export class SurveyQuestionAnalyticsPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly surveysApi = inject(SurveysApiService);

  surveyId = '';

  constructor() {
    // Register Chart.js components
    Chart.register(...registerables);
  }

  readonly analytics = signal<SurveyComprehensiveAnalyticsDto | null>(null);
  readonly failed = signal(false);
  readonly busy = signal(false);

  // Chart data
  readonly timelineChartData = signal<ChartData<'line'>>({ datasets: [] });
  readonly categoryChartData = signal<ChartData<'doughnut'>>({ datasets: [] });
  readonly ratingChartData = signal<ChartData<'pie'>>({ datasets: [] });
  readonly questionChartData = signal<ChartData<'bar'>>({ datasets: [] });
  
  // Individual question charts
  readonly questionChartsData = signal<Map<string, ChartData<any>>>(new Map());

  readonly chartOptions: ChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'bottom',
        labels: {
          padding: 15,
          font: {
            size: 12
          }
        }
      },
      tooltip: {
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        padding: 12,
        cornerRadius: 8,
        titleFont: {
          size: 14
        },
        bodyFont: {
          size: 13
        }
      }
    },
    elements: {
      line: {
        tension: 0.4,
        borderWidth: 3
      },
      point: {
        radius: 5,
        hoverRadius: 7
      }
    }
  };

  ngOnInit(): void {
    this.surveyId = this.route.snapshot.paramMap.get('surveyId') ?? '';
    this.load();
  }

  load(): void {
    if (!this.surveyId) return;
    this.busy.set(true);
    this.failed.set(false);
    this.surveysApi.getComprehensiveAnalytics(this.surveyId).subscribe({
      next: (data) => {
        this.analytics.set(data);
        this.setupCharts(data);
        this.busy.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.busy.set(false);
      },
    });
  }

  private setupCharts(data: SurveyComprehensiveAnalyticsDto): void {
    console.log('Analytics data received:', data);
    
    // Timeline chart - ensure we have data
    const timelineLabels = data.responseTimeline.length > 0 ? 
      data.responseTimeline.map(t => t.date) : ['No Data'];
    const timelineDataPoints = data.responseTimeline.length > 0 ? 
      data.responseTimeline.map(t => t.responseCount) : [0];
      
    const timelineData = {
      labels: timelineLabels,
      datasets: [{
        label: this.getChartLabel('responsesOverTime'),
        data: timelineDataPoints,
        borderColor: '#8B5CF6',
        backgroundColor: 'rgba(139, 92, 246, 0.15)',
        tension: 0.4,
        fill: true,
        borderWidth: 3,
        pointBackgroundColor: '#8B5CF6',
        pointBorderColor: '#fff',
        pointBorderWidth: 2,
        pointRadius: 6,
        pointHoverRadius: 8
      }],
    };
    console.log('Timeline chart data:', timelineData);
    this.timelineChartData.set(timelineData);

    // Category chart - ensure we have data
    const categoryLabels = data.categories.length > 0 ? 
      data.categories.map(c => c.categoryName) : ['No Categories'];
    const categoryDataPoints = data.categories.length > 0 ? 
      data.categories.map(c => c.responseCount) : [1];
      
    const categoryData = {
      labels: categoryLabels,
      datasets: [{
        data: categoryDataPoints,
        backgroundColor: [
          '#8B5CF6',
          '#EC4899',
          '#F59E0B',
          '#10B981',
          '#06B6D4',
          '#F97316',
        ],
        borderWidth: 2,
        borderColor: '#fff',
        hoverOffset: 8
      }],
    };
    console.log('Category chart data:', categoryData);
    this.categoryChartData.set(categoryData);

    // Rating chart
    if (data.ratings.length > 0) {
      const ratingData = {
        labels: data.ratings.map(r => `${r.rating} Stars`),
        datasets: [{
          data: data.ratings.map(r => r.count),
          backgroundColor: [
            '#EC4899',
            '#F97316',
            '#F59E0B',
            '#10B981',
            '#06B6D4',
          ],
          borderWidth: 2,
          borderColor: '#fff',
          hoverOffset: 10
        }],
      };
      console.log('Rating chart data:', ratingData);
      this.ratingChartData.set(ratingData);
    } else {
      console.log('No rating data available');
    }

    // Question answers chart - ensure we have data
    const topQuestions = data.questions.length > 0 ? data.questions.slice(0, 5) : 
      [{
        questionId: 'no-questions',
        titleEn: 'No Questions',
        titleAr: 'لا توجد أسئلة',
        questionType: 'ShortText',
        totalAnswers: 0,
        answerDistribution: [],
        averageRating: undefined,
        minRating: undefined,
        maxRating: undefined
      }];
    const questionData = {
      labels: topQuestions.map(q => {
        const title = this.getQuestionTitle(q);
        return title.length > 30 ? title.substring(0, 30) + '...' : title;
      }),
      datasets: [{
        label: this.getChartLabel('totalResponses'),
        data: topQuestions.map(q => q.totalAnswers),
        backgroundColor: '#06B6D4',
        borderColor: '#0891B2',
        borderWidth: 2,
        borderRadius: 8,
        hoverBackgroundColor: '#0891B2'
      }],
    };
    console.log('Question chart data:', questionData);
    this.questionChartData.set(questionData);
    
    // Create individual charts for each question
    this.createQuestionCharts(data.questions);
  }

  private createQuestionCharts(questions: QuestionAnalyticsDto[]): void {
    const chartsMap = new Map<string, ChartData<any>>();
    
    questions.forEach(question => {
      let chartData: ChartData<any>;
      
      if (question.questionType === 'Rating' || question.questionType === 'Scale') {
        // Use pie chart for ratings with modern colors
        chartData = {
          labels: question.answerDistribution.map((ad: AnswerDistributionDto) => ad.optionText),
          datasets: [{
            data: question.answerDistribution.map((ad: AnswerDistributionDto) => ad.count),
            backgroundColor: [
              '#EC4899',
              '#F97316', 
              '#F59E0B',
              '#10B981',
              '#06B6D4',
            ],
            borderWidth: 2,
            borderColor: '#fff',
            hoverOffset: 10
          }],
        };
      } else if (question.questionType === 'MultipleChoice' || question.questionType === 'SingleChoice') {
        // Use doughnut chart for multiple choice with modern colors
        chartData = {
          labels: question.answerDistribution.map((ad: AnswerDistributionDto) => ad.optionText),
          datasets: [{
            data: question.answerDistribution.map((ad: AnswerDistributionDto) => ad.count),
            backgroundColor: [
              '#8B5CF6',
              '#EC4899',
              '#F59E0B',
              '#10B981',
              '#06B6D4',
              '#F97316',
              '#14B8A6',
              '#A855F7',
            ],
            borderWidth: 2,
            borderColor: '#fff',
            hoverOffset: 8
          }],
        };
      } else if (question.questionType === 'YesNo') {
        // Use bar chart for yes/no with modern colors
        chartData = {
          labels: question.answerDistribution.map((ad: AnswerDistributionDto) => ad.optionText),
          datasets: [{
            label: 'Responses',
            data: question.answerDistribution.map((ad: AnswerDistributionDto) => ad.count),
            backgroundColor: ['#10B981', '#EF4444'],
            borderColor: ['#059669', '#DC2626'],
            borderWidth: 2,
            borderRadius: 6,
          }],
        };
      } else {
        // For text questions, show a modern bar with total responses
        chartData = {
          labels: ['Total Responses'],
          datasets: [{
            label: 'Responses',
            data: [question.totalAnswers],
            backgroundColor: '#06B6D4',
            borderColor: '#0891B2',
            borderWidth: 2,
            borderRadius: 8,
          }],
        };
      }
      
      chartsMap.set(question.questionId, chartData);
    });
    
    console.log('Individual question charts created:', chartsMap);
    this.questionChartsData.set(chartsMap);
  }

  getQuestionChart(questionId: string): ChartData<any> {
    const charts = this.questionChartsData();
    return charts.get(questionId) || { datasets: [] };
  }

  getQuestionChartType(questionType: string): any {
    switch (questionType) {
      case 'Rating':
      case 'Scale':
        return 'pie';
      case 'MultipleChoice':
      case 'SingleChoice':
        return 'doughnut';
      case 'YesNo':
        return 'bar';
      default:
        return 'bar';
    }
  }

  getQuestionTitle(question: QuestionAnalyticsDto): string {
    // This would ideally use the current language setting
    // For now, we'll use English as default
    return question.titleEn || question.titleAr || '';
  }

  getQuestionTypeLabel(questionType: string): string {
    const typeLabels: { [key: string]: { ar: string; en: string } } = {
      'ShortText': { ar: 'نص قصير', en: 'Short Text' },
      'LongText': { ar: 'نص طويل', en: 'Long Text' },
      'SingleChoice': { ar: 'اختيار واحد', en: 'Single Choice' },
      'MultipleChoice': { ar: 'اختيار متعدد', en: 'Multiple Choice' },
      'Rating': { ar: 'تقييم', en: 'Rating' },
      'Scale': { ar: 'مقياس', en: 'Scale' },
      'YesNo': { ar: 'نعم/لا', en: 'Yes/No' },
      'Number': { ar: 'رقم', en: 'Number' },
      'Date': { ar: 'تاريخ', en: 'Date' },
      'Email': { ar: 'بريد إلكتروني', en: 'Email' },
      'Phone': { ar: 'هاتف', en: 'Phone' },
      'Url': { ar: 'رابط', en: 'URL' }
    };

    const labels = typeLabels[questionType] || { ar: questionType, en: questionType };
    // This would ideally use the current language setting
    return labels.en; // Default to English for now
  }

  getChartLabel(key: string): string {
    const labels: { [key: string]: { ar: string; en: string } } = {
      'responsesOverTime': { ar: 'الردود بمرور الوقت', en: 'All Responses' },
      'totalResponses': { ar: 'إجمالي الردود', en: 'All Responses' },
      'average': { ar: 'المتوسط', en: 'Completion Rate' },
      'min': { ar: 'الحد الأدنى', en: 'Min' },
      'max': { ar: 'الحد الأقصى', en: 'Max' },
      'responses': { ar: 'ردود', en: 'Answers' }
    };

    const label = labels[key] || { ar: key, en: key };
    // This would ideally use the current language setting
    return label.en; // Default to English for now
  }
}
