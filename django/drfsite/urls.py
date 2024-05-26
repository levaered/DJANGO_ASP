from django.urls import path
from users.views import NoteListCreateAPIView, NoteDetailAPIView
from rest_framework_simplejwt.views import (
    TokenObtainPairView,
    TokenRefreshView,
)

urlpatterns = [
    path('notes/', NoteListCreateAPIView.as_view(), name='note-list'),
    path('notes/<int:pk>/', NoteDetailAPIView.as_view(), name='note-detail'),
    path('api/token/', TokenObtainPairView.as_view(), name='token_obtain_pair'),
    path('api/token/refresh/', TokenRefreshView.as_view(), name='token_refresh'),
]
