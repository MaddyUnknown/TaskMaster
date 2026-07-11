import {
  trigger,
  transition,
  style,
  query,
  group,
  animate,
} from '@angular/animations';

export const routeAnimations = trigger('routeAnimation', [
  transition('* <=> *', [
    style({ position: 'relative' }),
    query(':enter, :leave', [style({ position: 'absolute', width: '100%' })], {
      optional: true,
    }),
    group([
      query(
        ':leave',
        [
          style({
            opacity: 0,
            transform: 'translateY(-0.4rem)',
          }),
        ],
        { optional: true },
      ),
      query(
        ':enter',
        [
          style({ opacity: 0, transform: 'translateY(0.4rem)' }),
          animate(
            '250ms ease-out',
            style({ opacity: 1, transform: 'translateY(0)' }),
          ),
        ],
        { optional: true },
      ),
    ]),
  ]),
]);
