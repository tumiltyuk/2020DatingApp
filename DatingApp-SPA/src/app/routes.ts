import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { MemberListComponent } from './member-list/member-list.component';
import { MessagesComponent } from './messages/messages.component';
import { ListsComponent } from './lists/lists.component';
import { AuthGuard } from './_guards/auth.guard';

export const appRoutes: Routes = [  // routes apply on a first come first serve basis - ** Note ** the ordering of the items below.
    { path: '', component: HomeComponent }, // path '' is the same as localhost:4200
    {
      path: '', // localhost:4200/ + '' + pathFromBelow         -- ie localhost:4200/members
      runGuardsAndResolvers: 'always',
      canActivate: [AuthGuard],
      children: [
        { path: 'members', component: MemberListComponent, canActivate: [AuthGuard] },
        { path: 'messages', component: MessagesComponent },
        { path: 'lists', component: ListsComponent }
      ]
    },
    { path: '**', redirectTo: '', pathMatch: 'full' } // redirectTo '' is the same as localhost:4200
];
